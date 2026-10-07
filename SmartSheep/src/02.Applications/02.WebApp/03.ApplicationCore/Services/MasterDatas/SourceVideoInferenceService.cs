using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using InferenceYolo.Dis;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models.Inferences;
using Microsoft.Extensions.DependencyInjection;
using OpenCvSharp;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.Domain.Models;
using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Services.MasterDatas
{
    // Preview inferensi YOLO di halaman streaming SourceVideo.
    //
    // - Deteksi, centroid tracking, dan aturan menyusu memakai service InferenceYolo dengan section Yolo
    //   appsettings API yang sama dengan worker, sehingga tampilan konsisten dengan hitungan worker.
    // - Hanya tampilan: tidak ada file pending, sync, maupun penyimpanan ke database (data resmi dari worker).
    // - Frame beranotasi (bbox, centroid, track ID, status menyusu) dikirim sebagai MJPEG;
    //   status preview disimpan per previewId (satu tab = satu preview) dan dihapus saat tab ditutup.
    // - Statistik panel live dibaca dari file JSON pending worker (<Yolo:PendingFolder>/<kode kandang>/) untuk
    //   SourceVideo ini, bukan dihitung oleh preview.
    // - Rekaman diputar real time mengikuti waktu frame; stream langsung dibaca apa adanya. Bila sumber habis
    //   atau tidak bisa dibuka, preview diulang setelah RestartDelay.
    public sealed class SourceVideoInferenceService : ISourceVideoInferenceService, IDisposable
    {
        private const string EweLabel = "induk";
        private const string LambLabel = "anak";
        private const int PreviewWidth = 960;
        private const int JpegQuality = 70;
        private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(2);

        private readonly AppSetting appSetting;
        private readonly ISourceVideoStreamService streamService;
        private readonly Lazy<ServiceProvider> provider;
        private readonly ConcurrentDictionary<Guid, (SourceVideo SourceVideo, SourceVideoInferenceStatistic Preview)> previews = new();

        public SourceVideoInferenceService(AppSetting appSetting, ISourceVideoStreamService streamService)
        {
            this.appSetting = appSetting;
            this.streamService = streamService;
            provider = new Lazy<ServiceProvider>(() => new ServiceCollection()
                .AddInferenceServices(InferenceDi.ReadAppSetting(appSetting.YoloAppSettings))
                .BuildServiceProvider());
        }

        public SourceVideoInferenceStatistic GetStatistic(Guid previewId)
        {
            if (!previews.TryGetValue(previewId, out var preview))
            {
                return null;
            }

            var statistic = preview.Preview;
            var events = ReadWorkerEvents(preview.SourceVideo);
            var closed = events.Where(e => e.IsClosed).ToList();
            var sucklings = closed.Where(e => e.EventType == SucklingEvent.Suckling).ToList();

            return new SourceVideoInferenceStatistic
            {
                Status = statistic.Status,
                FrameAt = statistic.FrameAt,
                EweCount = statistic.EweCount,
                LambCount = statistic.LambCount,
                Fps = statistic.Fps,
                WorkerUpdatedAt = events.Count > 0 ? events.Max(e => e.EndAt) : null,
                WorkerFrom = events.Count > 0 ? events.Min(e => e.StartAt) : null,
                WorkerUntil = events.Count > 0 ? events.Max(e => e.EndAt) : null,
                SucklingNow = events.Where(e => !e.IsClosed && e.EventType == SucklingEvent.Suckling)
                    .Select(e => new SourceVideoInferenceSuckling { LambTrackId = e.LambTrackId, EweTrackId = e.EweTrackId, StartAt = e.StartAt, Seconds = e.DurationSeconds })
                    .ToList(),
                TotalApproach = closed.Count(e => e.EventType == SucklingEvent.Approach),
                TotalFrequency = sucklings.Count,
                TotalDurationSeconds = Math.Round(sucklings.Sum(e => e.DurationSeconds), 1),
                TotalFailedAttempt = closed.Count(e => e.EventType == SucklingEvent.FailedAttempt),
                LongestGapSeconds = events.Count > 0 ? GetLongestGapSeconds(events, events.Min(e => e.StartAt), events.Max(e => e.EndAt)) : 0
            };
        }

        // Jeda terlama tanpa menyusu dalam rentang data worker: dari awal data, antar kejadian menyusu (termasuk yang
        // masih berjalan), sampai data terakhir
        private static double GetLongestGapSeconds(List<SucklingEvent> events, DateTime from, DateTime to)
        {
            double longest = 0;
            var cursor = from;
            foreach (var suckling in events.Where(e => e.EventType == SucklingEvent.Suckling).OrderBy(e => e.StartAt))
            {
                longest = Math.Max(longest, (suckling.StartAt - cursor).TotalSeconds);
                if (suckling.EndAt > cursor)
                {
                    cursor = suckling.EndAt;
                }
            }

            return Math.Round(Math.Max(longest, (to - cursor).TotalSeconds), 1);
        }

        // File pending worker untuk SourceVideo ini; file yang sedang ditulis/di-sync worker dilewati
        private List<SucklingEvent> ReadWorkerEvents(SourceVideo sourceVideo)
        {
            string folder = Path.Combine(Path.GetFullPath(appSetting.YoloPendingFolder), sourceVideo.Barn?.Code ?? "");
            if (string.IsNullOrEmpty(sourceVideo.Barn?.Code) || !Directory.Exists(folder))
            {
                return new List<SucklingEvent>();
            }

            var events = new List<SucklingEvent>();
            foreach (string file in Directory.EnumerateFiles(folder, "*.json"))
            {
                try
                {
                    var sucklingEvent = JsonSerializer.Deserialize<SucklingEvent>(File.ReadAllText(file));
                    if (sucklingEvent?.SourceVideoCode == sourceVideo.Code)
                    {
                        events.Add(sucklingEvent);
                    }
                }
                catch (Exception ex) when (ex is IOException or JsonException)
                {
                    // File sedang diganti atau dihapus worker
                }
            }

            return events;
        }

        public async Task WriteFramesAsync(SourceVideo sourceVideo, Guid previewId, Stream output, CancellationToken cancellationToken)
        {
            previews[previewId] = (sourceVideo, new SourceVideoInferenceStatistic());
            var owner = previews[previewId];
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await RunAsync(sourceVideo, previewId, output, cancellationToken);
                        previews[previewId] = (sourceVideo, new SourceVideoInferenceStatistic { Status = "Mengulang" });
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or OpenCVException or IOException)
                    {
                        previews[previewId] = (sourceVideo, new SourceVideoInferenceStatistic { Status = $"Sumber tidak bisa dibuka: {ex.Message}" });
                    }

                    await Task.Delay(RestartDelay, cancellationToken);
                }
            }
            finally
            {
                // Hanya request ini yang menghapus entrinya (browser bisa membuka stream yang sama dua kali)
                if (previews.TryGetValue(previewId, out var current) && ReferenceEquals(current.SourceVideo, owner.SourceVideo))
                {
                    previews.TryRemove(previewId, out _);
                }
            }
        }

        private async Task RunAsync(SourceVideo sourceVideo, Guid previewId, Stream output, CancellationToken cancellationToken)
        {
            using var scope = provider.Value.CreateScope();
            var option = scope.ServiceProvider.GetRequiredService<InferenceOption>();
            option.SourceVideoCode = sourceVideo.Code;
            option.Model = Path.GetFullPath(appSetting.YoloModel);
            option.Labels = Path.GetFullPath(appSetting.YoloLabels);
            option.UseCoreMl = bool.TryParse(appSetting.YoloUseCoreMl, out bool useCoreMl) && useCoreMl;
            option.IsOffline = true;
            option.SaveVideo = false;

            var source = scope.ServiceProvider.GetRequiredService<IVideoSourceService>();
            source.Open(streamService.GetInput(sourceVideo));
            using var stop = cancellationToken.Register(source.Stop);

            var detector = scope.ServiceProvider.GetRequiredService<IYoloDetectorService>();
            var tracker = scope.ServiceProvider.GetRequiredService<ICentroidTrackerService>();
            var analyzer = scope.ServiceProvider.GetRequiredService<ISucklingAnalyzerService>();
            var annotator = scope.ServiceProvider.GetRequiredService<IFrameAnnotatorService>();

            var clock = Stopwatch.StartNew();
            var encodeParams = new[] { new ImageEncodingParam(ImwriteFlags.JpegQuality, JpegQuality) };
            using var frame = new Mat();
            using var resized = new Mat();
            double? firstSeconds = null;
            int processed = 0;

            while (source.Read(frame, out double timeSeconds))
            {
                var detections = detector.Detect(frame);

                var analysis = analyzer.Analyze(tracker.Update(detections), timeSeconds, source.StartedAt.AddSeconds(timeSeconds), frame.Size());
                annotator.Draw(frame, analysis);

                var image = frame;
                if (frame.Width > PreviewWidth)
                {
                    Cv2.Resize(frame, resized, new Size(PreviewWidth, frame.Height * PreviewWidth / frame.Width), 0, 0, InterpolationFlags.Area);
                    image = resized;
                }
                Cv2.ImEncode(".jpg", image, out byte[] jpeg, encodeParams);
                await streamService.WriteFrameAsync(output, jpeg, cancellationToken);

                processed++;
                previews[previewId] = (sourceVideo, new SourceVideoInferenceStatistic
                {
                    Status = source.IsStream ? "Live" : "Rekaman",
                    FrameAt = analysis.FrameAt,
                    EweCount = analysis.Objects.Count(o => o.IsConfirmed && o.IsVisible && o.Label == EweLabel),
                    LambCount = analysis.Objects.Count(o => o.IsConfirmed && o.IsVisible && o.Label == LambLabel),
                    Fps = Math.Round(processed / clock.Elapsed.TotalSeconds, 1)
                });

                // Rekaman diputar real time mengikuti waktu frame
                if (!source.IsStream)
                {
                    firstSeconds ??= timeSeconds;
                    var wait = TimeSpan.FromSeconds(timeSeconds - firstSeconds.Value) - clock.Elapsed;
                    if (wait > TimeSpan.Zero)
                    {
                        await Task.Delay(wait, cancellationToken);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (provider.IsValueCreated)
            {
                provider.Value.Dispose();
            }
        }
    }
}
