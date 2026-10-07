using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using InferenceYolo.Dis;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using InferenceYolo.Services.Inferences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Format angka dan tanggal konsisten (titik desimal, HH:mm:ss) di semua locale
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

InferenceOption option;
AppSetting setting;
try
{
    option = ParseOption(args);
    setting = InferenceDi.ReadAppSetting(option.AppSettings);
}
catch (Exception ex) when (ex is ArgumentException or FormatException or FileNotFoundException or InvalidDataException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

// Worker background: semua SourceVideo online diproses terus-menerus (dijalankan service manager OS)
if (option.IsService)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Services.AddWindowsService(o => o.ServiceName = "SmartSheep InferenceYolo");
    builder.Services.AddSystemd();
    builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromMinutes(1));
    builder.Services.AddInferenceServices(setting);
    builder.Services.AddHostedService(sp => new InferenceWorkerService(sp, option, setting));
    builder.Build().Run();
    return 0;
}

// Mode manual: satu file video, folder rekaman, URL live streaming, atau --source
using var provider = new ServiceCollection()
    .AddInferenceServices(setting)
    .BuildServiceProvider();
using var scope = provider.CreateScope();
option.CopyTo(scope.ServiceProvider.GetRequiredService<InferenceOption>());

// Ctrl+C (SIGINT) atau SIGTERM: hentikan dengan rapi agar kejadian yang berjalan ditutup dan di-sync
using var cancellation = new CancellationTokenSource();
void Cancel(PosixSignalContext context)
{
    context.Cancel = true;
    cancellation.Cancel();
}
using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, Cancel);
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, Cancel);

SucklingStatistic result;
try
{
    result = scope.ServiceProvider.GetRequiredService<IInferenceRunnerService>().Run(cancellation.Token);
}
catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

string statisticPath = Path.Combine(option.Output, string.Concat(option.SourceVideoCode.Split(Path.GetInvalidFileNameChars())) + ".statistic.json");
File.WriteAllText(statisticPath, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"  Periode             : {result.ActivityFrom:yyyy-MM-dd HH:mm:ss} - {result.ActivityTo:HH:mm:ss} ({result.TimeCategory}, jam {result.HourNumber})");
Console.WriteLine($"  Frekuensi pendekatan: {result.TotalApproach}");
Console.WriteLine($"  Jumlah menyusu      : {result.TotalFrequency} ({result.TotalDurationSeconds:0.0} detik)");
Console.WriteLine($"  Pendekatan gagal    : {result.TotalFailedAttempt}");
Console.WriteLine($"  Jeda terpanjang     : {result.LongestGapSeconds:0.0} detik");
Console.WriteLine($"Output : {option.Output}/");
return 0;

static InferenceOption ParseOption(string[] args)
{
    var option = new InferenceOption();

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{arg} butuh nilai.");

        switch (arg.ToLowerInvariant())
        {
            case "--source":
                option.SourceVideoCode = Next();
                break;
            case "-o":
            case "--output":
                option.Output = Next();
                break;
            case "--model":
                option.Model = Next();
                break;
            case "--labels":
                option.Labels = Next();
                break;
            case "--appsettings":
                option.AppSettings = Next();
                break;
            case "--no-video":
                option.SaveVideo = false;
                break;
            case "--trace":
                option.Trace = true;
                break;
            case "--coreml":
                option.UseCoreMl = true;
                break;
            case "--offline":
                option.IsOffline = true;
                break;
            case "--service":
                option.IsService = true;
                break;
            case "--max-frames":
                option.MaxFrames = int.Parse(Next(), CultureInfo.InvariantCulture);
                if (option.MaxFrames < 1)
                {
                    throw new ArgumentException("--max-frames minimal 1.");
                }
                break;
            case "-h":
            case "--help":
                PrintHelp();
                Environment.Exit(0);
                break;
            default:
                if (arg.StartsWith('-'))
                {
                    throw new ArgumentException($"Opsi tidak dikenal: {arg}");
                }
                option.Input = arg;
                break;
        }
    }

    foreach (string path in new[] { option.Model, option.Labels })
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File tidak ditemukan: {Path.GetFullPath(path)}");
        }
    }

    if (option.IsService)
    {
        return option;
    }

    if (string.IsNullOrWhiteSpace(option.Input) && string.IsNullOrWhiteSpace(option.SourceVideoCode))
    {
        PrintHelp();
        throw new ArgumentException("Input (file video, folder rekaman, atau URL live streaming), --source, atau --service wajib diisi.");
    }

    if (string.IsNullOrWhiteSpace(option.SourceVideoCode))
    {
        option.SourceVideoCode = option.Input.Contains("://", StringComparison.Ordinal) ? "STREAM"
            : Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(option.Input));
    }

    return option;
}

static void PrintHelp()
{
    Console.WriteLine("""
InferenceYolo [<input>] [opsi]
InferenceYolo --service [opsi]

  <input>               file video, folder rekaman (diproses bersambung urut waktu),
                        atau URL live streaming (rtsp://, http://). Tanpa <input>, URL kamera
                        diambil dari SourceVideo di API berdasarkan --source.
  --source KODE         kode SourceVideo (kandang hasil sync ditentukan dari kode ini;
                        default: nama file/folder, atau STREAM)
  -o, --output DIR      folder output video, trace, dan statistik (default: output)
  --model PATH          model ONNX (default: ../smartsheep.onnx)
  --labels PATH         nama kelas (default: ../smartsheep.labels.json)
  --appsettings PATH    appsettings API berisi section Yolo (default: ../../../04.Api/appsettings.json)
  --no-video            tidak menulis video beranotasi (lebih cepat, disarankan untuk live streaming)
  --trace               tulis status aturan menyusu per anak per frame ke <output>/<kode>.trace.csv
  --coreml              pakai CoreML execution provider (macOS)
  --offline             tidak sync ke API; kejadian ditulis ke <output>/synced_events.csv
  --max-frames N        proses maksimal N frame (untuk uji coba)
  --service             worker background: semua SourceVideo online (aktif, punya kandang) diproses
                        terus-menerus, daftar diperbarui tiap Yolo:SourceRefreshSeconds; dipasang sebagai
                        Windows Service / systemd / launchd (path relatif dihitung dari working directory)

Kejadian ditulis ke Yolo:PendingFolder/<kode kandang>/ lalu di-sync ke API (Yolo:ApiUrl) bila tidak ada indikasi menyusu selama
Yolo:SyncIdleSeconds.
Live streaming yang putus disambung ulang otomatis (jeda 2 detik, naik bertahap sampai 30 detik).
Ctrl+C / SIGTERM menghentikan proses dengan rapi (kejadian yang berjalan ditutup dan di-sync).

Contoh:
  dotnet run -c Release -- ".../DATASET/2025/08/08/10/20250808.100001.60637.avi"
  dotnet run -c Release -- ".../DATASET/2025/08/08/10" --no-video
  dotnet run -c Release -- rtsp://user:pass@kamera/stream --source CAM-01 --no-video
  dotnet run -c Release -- --source SRC-PG-01-01 --coreml --no-video
  dotnet run -c Release -- --service --coreml
""");
}
