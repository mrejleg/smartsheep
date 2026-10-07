using System.Globalization;
using System.Text.Json;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InferenceYolo.Services.Inferences
{
    // Worker background (--service): dijalankan service manager OS (Windows Service / systemd / launchd).
    //
    // - Daftar SourceVideo online diambil dari API (SourceVideo aktif dengan FK Barn) setiap
    //   Yolo:SourceRefreshSeconds; Yolo:SourceCodes (dipisah koma) membatasi kamera yang diproses.
    // - Satu runner per SourceVideo di thread prioritas rendah (scope DI sendiri) agar komputer tetap ringan.
    //   Kamera baru otomatis dijalankan, kamera yang tidak lagi online/aktif dihentikan dengan rapi.
    // - Runner yang berhenti (stream tidak bisa dibuka atau error) dijalankan ulang setelah RestartDelay.
    // - File pending sisa proses sebelumnya (semua kandang) di-sync sekali saat worker mulai.
    public sealed class InferenceWorkerService : BackgroundService
    {
        private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(30);

        private readonly IServiceProvider provider;
        private readonly InferenceOption template;
        private readonly AppSetting setting;
        private readonly Dictionary<string, (Thread Thread, CancellationTokenSource Cancellation)> runners = new();

        public InferenceWorkerService(IServiceProvider provider, InferenceOption template, AppSetting setting)
        {
            this.provider = provider;
            this.template = template;
            this.setting = setting;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var refresh = TimeSpan.FromSeconds(double.Parse(setting.YoloSourceRefreshSeconds, CultureInfo.InvariantCulture));
            var allowedCodes = (setting.YoloSourceCodes ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            await Task.Yield();
            RecoverPending();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Refresh(allowedCodes);
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
                {
                    Console.Error.WriteLine($"Daftar SourceVideo: {ex.Message}");
                }

                try
                {
                    await Task.Delay(refresh, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            foreach (string code in runners.Keys.ToList())
            {
                Stop(code);
            }
        }

        private void Refresh(HashSet<string> allowedCodes)
        {
            List<SyncSource> sources;
            using (var scope = CreateScope(template.SourceVideoCode, template.BarnCode))
            {
                sources = scope.ServiceProvider.GetRequiredService<ISucklingSyncService>().GetSources()
                    .Where(s => allowedCodes.Count == 0 || allowedCodes.Contains(s.Code))
                    .ToList();
            }

            var codes = sources.Select(s => s.Code).ToHashSet();
            foreach (string code in runners.Keys.Where(c => !codes.Contains(c)).ToList())
            {
                Stop(code);
            }

            foreach (var source in sources.Where(s => !runners.ContainsKey(s.Code)))
            {
                Start(source);
            }
        }

        private void Start(SyncSource source)
        {
            var cancellation = new CancellationTokenSource();
            var thread = new Thread(() => RunLoop(source, cancellation.Token))
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = $"Inference-{source.Code}"
            };

            runners[source.Code] = (thread, cancellation);
            thread.Start();
            Console.WriteLine($"[{source.Code}] dijalankan (kandang {source.BarnCode})");
        }

        // Runner dihentikan dengan rapi: kejadian berjalan ditutup dan di-sync sebelum thread selesai
        private void Stop(string code)
        {
            if (!runners.Remove(code, out var runner))
            {
                return;
            }

            runner.Cancellation.Cancel();
            runner.Thread.Join();
            runner.Cancellation.Dispose();
            Console.WriteLine($"[{code}] dihentikan");
        }

        private void RunLoop(SyncSource source, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = CreateScope(source.Code, source.BarnCode);
                    var option = scope.ServiceProvider.GetRequiredService<InferenceOption>();
                    option.Input = source.SourceVideoUrl;

                    var result = scope.ServiceProvider.GetRequiredService<IInferenceRunnerService>().Run(cancellationToken);
                    Console.WriteLine($"[{source.Code}] stream berhenti: menyusu {result.TotalFrequency} ({result.TotalDurationSeconds:0.0} detik), " +
                        $"pendekatan {result.TotalApproach}, gagal {result.TotalFailedAttempt}");
                }
                catch (Exception ex)
                {
                    // Satu kamera bermasalah tidak boleh menghentikan worker; dicoba lagi setelah RestartDelay
                    Console.Error.WriteLine($"[{source.Code}] {ex.Message}");
                }

                cancellationToken.WaitHandle.WaitOne(RestartDelay);
            }
        }

        // File pending sisa proses sebelumnya: folder per kandang, dikelompokkan per SourceVideo
        private void RecoverPending()
        {
            string root = Path.GetFullPath(setting.YoloPendingFolder);
            if (!Directory.Exists(root))
            {
                return;
            }

            var leftovers = Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                .Select(file => (
                    BarnCode: Path.GetFileName(Path.GetDirectoryName(file))!,
                    SourceVideoCode: JsonSerializer.Deserialize<SucklingEvent>(File.ReadAllText(file))!.SourceVideoCode))
                .Distinct()
                .ToList();

            foreach (var (barnCode, sourceVideoCode) in leftovers)
            {
                using var scope = CreateScope(sourceVideoCode, barnCode);
                int synced = scope.ServiceProvider.GetRequiredService<IPendingEventService>().SyncAll(null);
                Console.WriteLine($"[{sourceVideoCode}] sync {synced} kejadian sisa proses sebelumnya");
            }
        }

        private IServiceScope CreateScope(string sourceVideoCode, string barnCode)
        {
            var scope = provider.CreateScope();
            var option = scope.ServiceProvider.GetRequiredService<InferenceOption>();
            template.CopyTo(option);
            option.SourceVideoCode = sourceVideoCode;
            option.BarnCode = barnCode;
            option.IsService = true;
            option.SaveVideo = false;
            option.Trace = false;
            option.MaxFrames = null;
            return scope;
        }
    }
}
