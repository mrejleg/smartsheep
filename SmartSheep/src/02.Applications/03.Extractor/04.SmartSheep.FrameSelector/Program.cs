using System.Globalization;
using Extractor.ApplicationCore.Dis;
using Extractor.ApplicationCore.Interfaces.Extractors;
using Extractor.Domain.Models.Extractors;
using Microsoft.Extensions.DependencyInjection;

ExtractorOption option;
try
{
    option = ParseOption(args);
}
catch (Exception ex) when (ex is ArgumentException or FormatException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

using var provider = new ServiceCollection()
    .AddExtractorServices()
    .BuildServiceProvider();
var extractor = provider.GetRequiredService<IFrameExtractorService>();

string input = Path.GetFullPath(option.Input);
List<string> videos;
try
{
    videos = extractor.GetVideos(input);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

if (option.Limit is int limit)
{
    videos = videos.Take(limit).ToList();
}

if (videos.Count == 0)
{
    Console.Error.WriteLine($"Tidak ada file video di {input}");
    return 1;
}

int total = 0;
int failed = 0;
for (int i = 0; i < videos.Count; i++)
{
    string video = videos[i];
    string outputFolder = extractor.GetOutputFolder(input, video, option.Output);
    string tag = $"[{i + 1}/{videos.Count}] {Path.GetRelativePath(Directory.Exists(input) ? input : Path.GetDirectoryName(input)!, video)}";

    if (option.SkipExisting && extractor.IsExtracted(outputFolder))
    {
        Console.WriteLine($"{tag}: dilewati (sudah ada)");
        continue;
    }

    try
    {
        var result = extractor.Extract(video, outputFolder, option);
        total += result.Saved;

        string info = $", {result.NamedFromOverlay} nama dari jam overlay";
        if (result.ExpectedFrameCount > 0 && result.FrameCount < result.ExpectedFrameCount)
        {
            info += $" (peringatan: hanya {result.FrameCount} dari {result.ExpectedFrameCount} frame terbaca)";
        }
        if (result.NamedFromOverlay < result.Saved && !result.RecordedAtFromFileName)
        {
            info += " (peringatan: waktu tidak ada di nama file, pakai waktu modifikasi file)";
        }

        Console.WriteLine($"{tag}: {result.Fps:0.##} fps, {result.FrameCount} frame -> {result.Saved} gambar{info}");
    }
    catch (Exception ex) // satu video rusak jangan hentikan semua
    {
        failed++;
        Console.WriteLine($"{tag}: GAGAL - {ex.Message}");
    }
}

Console.WriteLine($"Selesai: {total} gambar di {option.Output}/, {failed} video gagal");
return failed > 0 ? 2 : 0;

static ExtractorOption ParseOption(string[] args)
{
    var option = new ExtractorOption();

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{arg} butuh nilai.");

        switch (arg.ToLowerInvariant())
        {
            case "-o":
            case "--output":
                option.Output = Next();
                break;
            case "--interval":
                option.Interval = double.Parse(Next(), CultureInfo.InvariantCulture);
                break;
            case "--ext":
                option.Ext = Next().ToLowerInvariant();
                if (option.Ext is not ("jpg" or "png"))
                {
                    throw new ArgumentException("--ext harus jpg atau png.");
                }
                break;
            case "--quality":
                option.Quality = int.Parse(Next(), CultureInfo.InvariantCulture);
                if (option.Quality is < 0 or > 100)
                {
                    throw new ArgumentException("--quality harus 0-100.");
                }
                break;
            case "--limit":
                option.Limit = int.Parse(Next(), CultureInfo.InvariantCulture);
                if (option.Limit < 1)
                {
                    throw new ArgumentException("--limit minimal 1.");
                }
                break;
            case "--skip-existing":
                option.SkipExisting = true;
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

    if (string.IsNullOrWhiteSpace(option.Input))
    {
        PrintHelp();
        throw new ArgumentException("Input (file video atau folder) wajib diisi.");
    }

    if (option.Interval <= 0)
    {
        PrintHelp();
        throw new ArgumentException("--interval wajib diisi dan lebih dari 0 (mis. --interval 1).");
    }

    return option;
}

static void PrintHelp()
{
    Console.WriteLine("""
SmartSheep.FrameSelector <input> --interval DETIK [opsi]

  <input>               file video atau folder (dicari rekursif)
  --interval DETIK      wajib: 1 gambar tiap N detik (mis. 1). fps dicek otomatis per video,
                        lalu diambil tiap fps x DETIK frame (20 fps -> tiap 20, 10 fps -> tiap 10)
  -o, --output DIR      folder output (default: frames)
  --ext jpg|png         format gambar (default: jpg)
  --quality N           kualitas JPG 0-100 (default: 95)
  --limit N             proses maksimal N video (untuk uji coba)
  --skip-existing       lewati video yang sudah selesai diekstrak sebelumnya

Contoh:
  SmartSheep.FrameSelector ".../DATASET/2025/08/11" --interval 1 -o frames --quality 85 --skip-existing
  SmartSheep.FrameSelector ".../DATASET/2025/08/11/10" --interval 1 -o frames --limit 1
""");
}
