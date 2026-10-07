using System.Diagnostics;
using System.Globalization;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;

namespace InferenceYolo.Services.Inferences
{
    // Pipeline inferensi satu SourceVideo: baca frame -> deteksi YOLO -> centroid tracking -> aturan menyusu
    // -> file pending -> sync ke API. Dipakai mode manual (Program) dan worker background (satu runner per
    // SourceVideo dalam scope DI sendiri). Detector baru dimuat setelah sumber video berhasil dibuka agar
    // kamera yang offline tidak memakan memori.
    public class InferenceRunnerService : IInferenceRunnerService
    {
        private const int ProgressInterval = 200;

        private readonly IServiceProvider provider;
        private readonly InferenceOption option;
        private readonly AppSetting setting;

        public InferenceRunnerService(IServiceProvider provider, InferenceOption option, AppSetting setting)
        {
            this.provider = provider;
            this.option = option;
            this.setting = setting;
        }

        public SucklingStatistic Run(CancellationToken cancellationToken)
        {
            ResolveSource();

            var pending = provider.GetRequiredService<IPendingEventService>();
            int recovered = pending.SyncAll(null);
            if (recovered > 0)
            {
                Log($"Sync {recovered} kejadian sisa proses sebelumnya");
            }

            var source = provider.GetRequiredService<IVideoSourceService>();
            source.Open(option.Input);
            using var stop = cancellationToken.Register(source.Stop);

            var detector = provider.GetRequiredService<IYoloDetectorService>();
            var tracker = provider.GetRequiredService<ICentroidTrackerService>();
            var analyzer = provider.GetRequiredService<ISucklingAnalyzerService>();
            var annotator = provider.GetRequiredService<IFrameAnnotatorService>();
            var statistic = provider.GetRequiredService<ISucklingStatisticService>();

            string outputName = GetOutputName();
            using var writer = option.SaveVideo ? CreateWriter(source, outputName) : null;
            using var trace = option.Trace ? CreateTrace(outputName) : null;

            Log($"Input  : {(source.IsStream ? option.SourceVideoCode : option.Input)} ({source.FrameSize.Width}x{source.FrameSize.Height}, " +
                $"{(source.IsStream ? "live streaming" : source.VideoCount > 1 ? $"{source.VideoCount} klip bersambung" : $"{source.Fps:0.##} fps, {source.FrameCount} frame")}, " +
                $"kandang {(string.IsNullOrWhiteSpace(option.BarnCode) ? "-" : option.BarnCode)}, mulai {source.StartedAt:yyyy-MM-dd HH:mm:ss})");
            Log($"Model  : {option.Model} (input {detector.InputSize}, {(option.UseCoreMl ? "CoreML" : "CPU")}, " +
                $"confidence >= {setting.YoloConfidenceThreshold}, menyusu > {setting.YoloMinimumSucklingSeconds} detik, maks {setting.YoloMaximumProcessFps} fps)");
            Log($"Sync   : {(option.IsOffline ? $"offline ({option.Output}/synced_events.csv)" : setting.YoloApiUrl)}");

            var events = new List<SucklingEvent>();
            var total = Stopwatch.StartNew();
            var inference = new Stopwatch();
            using var frame = new Mat();
            double lastSeconds = 0;
            DateTime lastFrameAt = source.StartedAt;
            int processed = 0;
            int reconnects = 0;

            while ((option.MaxFrames is not int max || processed < max) && source.Read(frame, out double timeSeconds))
            {
                // Stream sempat putus: objek dianggap hilang, kejadian berhenti di frame terakhir sebelum putus
                if (source.ReconnectCount != reconnects)
                {
                    reconnects = source.ReconnectCount;
                    Save(analyzer.Complete(), events, pending);
                }

                inference.Start();
                var detections = detector.Detect(frame);
                inference.Stop();

                var tracks = tracker.Update(detections);
                var analysis = analyzer.Analyze(tracks, timeSeconds, source.StartedAt.AddSeconds(timeSeconds), frame.Size());

                Save(analysis.ClosedEvents, events, pending);
                foreach (var active in analysis.ActiveEvents)
                {
                    pending.Save(active);
                }
                pending.SyncIfIdle(analysis.FrameAt, timeSeconds, analysis.ActiveEvents.Count > 0);

                if (trace != null)
                {
                    WriteTrace(trace, analysis);
                }

                if (writer != null)
                {
                    annotator.Draw(frame, analysis);
                    writer.Write(frame);
                }

                lastSeconds = timeSeconds;
                lastFrameAt = analysis.FrameAt;
                processed++;
                if (!option.IsService && processed % ProgressInterval == 0)
                {
                    string progress = source.FrameCount > 0 ? $"{processed}/{source.FrameCount} frame" : $"{processed} frame";
                    Log($"  {progress} | {processed / total.Elapsed.TotalSeconds:0.0} fps | inferensi {inference.Elapsed.TotalMilliseconds / processed:0.0} ms/frame | " +
                        $"menyusu {events.Count(e => e.EventType == SucklingEvent.Suckling)}");
                }
            }

            Save(analyzer.Complete(), events, pending);
            pending.SyncAll(lastFrameAt);

            Log($"Selesai: {processed} frame dalam {total.Elapsed.TotalSeconds:0.0} detik ({processed / Math.Max(total.Elapsed.TotalSeconds, 1e-9):0.0} fps)");
            return statistic.Calculate(option.SourceVideoCode, events, source.StartedAt, lastFrameAt.AddSeconds(source.Fps > 0 ? 1 / source.Fps : 0));
        }

        // Kandang (dan URL bila input kosong) diambil dari SourceVideo di API; offline memakai input apa adanya
        private void ResolveSource()
        {
            if (option.IsOffline || (!string.IsNullOrWhiteSpace(option.BarnCode) && !string.IsNullOrWhiteSpace(option.Input)))
            {
                return;
            }

            var source = provider.GetRequiredService<ISucklingSyncService>().GetSource(option.SourceVideoCode);
            option.BarnCode = source.BarnCode;
            if (string.IsNullOrWhiteSpace(option.Input))
            {
                option.Input = source.SourceVideoUrl;
            }
        }

        private static void Save(List<SucklingEvent> closedEvents, List<SucklingEvent> events, IPendingEventService pending)
        {
            foreach (var closed in closedEvents)
            {
                events.Add(closed);
                pending.Save(closed);
            }
        }

        private string GetOutputName()
        {
            Directory.CreateDirectory(option.Output);
            return Path.Combine(option.Output, string.Concat(option.SourceVideoCode.Split(Path.GetInvalidFileNameChars())));
        }

        // fps output = fps yang benar-benar diproses; fps stream sering tidak terbaca (pakai 20 sebagai pendekatan)
        private VideoWriter CreateWriter(IVideoSourceService source, string outputName)
        {
            double fps = source.Fps > 0 ? source.Fps : 20;
            double maximumProcessFps = double.Parse(setting.YoloMaximumProcessFps, CultureInfo.InvariantCulture);
            if (maximumProcessFps > 0 && fps > maximumProcessFps)
            {
                fps /= Math.Round(fps / maximumProcessFps);
            }

            return new VideoWriter(outputName + ".mp4", FourCC.FromString("mp4v"), fps, source.FrameSize);
        }

        private static StreamWriter CreateTrace(string outputName)
        {
            var trace = new StreamWriter(outputName + ".trace.csv");
            trace.WriteLine("time_s,lamb_track_id,ewe_track_id,visible,moving,overlap,in_udder_zone,ratio,baseline_ratio,area,baseline_area,standing,shrunk,stay_point,suckling");
            return trace;
        }

        private static void WriteTrace(StreamWriter trace, FrameAnalysis analysis)
        {
            string time = analysis.TimeSeconds.ToString("0.000", CultureInfo.InvariantCulture);
            foreach (var l in analysis.Lambs)
            {
                trace.WriteLine(string.Join(',', time, l.LambTrackId, l.EweTrackId, B(l.IsVisible), B(l.IsMoving), B(l.IsOverlap),
                    B(l.IsInUdderZone), F(l.Ratio), F(l.BaselineRatio), F(l.Area), F(l.BaselineArea), B(l.IsStanding), B(l.IsShrunk),
                    B(l.IsStayPoint), B(l.IsSuckling)));
            }

            static string B(bool value) => value ? "1" : "0";
            static string F(float? value) => value?.ToString("0.###", CultureInfo.InvariantCulture) ?? "";
        }

        private void Log(string message)
        {
            Console.WriteLine(option.IsService ? $"[{option.SourceVideoCode}] {message}" : message);
        }
    }
}
