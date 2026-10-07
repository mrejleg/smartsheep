using System.Globalization;
using System.Text.RegularExpressions;
using Extractor.ApplicationCore.Interfaces.Extractors;
using Extractor.Domain.Models.Extractors;
using Extractor.Infrastructure.FFmpegs;
using OpenCvSharp;

namespace Extractor.ApplicationCore.Services.Extractors
{
    // Ekstraksi frame video SMARTSHEEP.
    //
    // fps dicek dulu per video (ffprobe; CCTV bisa 20 atau 10 fps), lalu ambil tiap
    // fps x --interval frame (20 fps x 1 detik = frame ke-0, 20, 40, ...; 10 fps = 0, 10, 20, ...).
    // Nama file = jam overlay kamera di pojok kiri atas ("BARDI 2025-08-11 10:00:01"), dibaca
    // dengan OpenCV (template matching angka 0-9): yyyy_MM_dd_HH_mm_ss.jpg (24 jam).
    // Bila overlay tidak terbaca, nama dihitung dari waktu di nama file video.
    //
    // Output mengikuti struktur folder input:
    //   frames/10/20250811.100002.60222/2025_08_11_10_00_01.jpg
    public class FrameExtractorService : IFrameExtractorService
    {
        // Ditulis setelah satu video selesai diekstrak; --skip-existing hanya melewati
        // video yang punya penanda ini, sehingga video yang terputus di tengah diulang.
        private const string DoneMarker = ".done";

        private static readonly HashSet<string> VideoExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".avi", ".mp4", ".mov", ".mkv", ".m4v" };

        // Posisi overlay pada frame 1920x1080: font monospace, karakter ke-i mulai di x = 107 + 26*i.
        // "BARDI 2025-08-11 10:00:01" -> angka tanggal & jam ada di karakter berikut.
        private const int OverlayWidth = 1920;
        private const int OverlayHeight = 1080;
        private const int CellX = 107;
        private const int CellY = 76;
        private const int CellPitch = 26;
        private const int CellWidth = 26;
        private const int CellHeight = 36;
        private const int CellPad = 3;
        private static readonly int[] DigitCells = { 6, 7, 8, 9, 11, 12, 14, 15, 17, 18, 20, 21, 23, 24 };

        // Batas kecocokan template (diukur pada data 2025-08-11: angka benar >= 0,5 dan
        // selisih dengan angka kedua terbaik >= 0,198).
        private const double MinScore = 0.5;
        private const double MinMargin = 0.1;

        // Jam overlay pertama harus dekat dengan waktu di nama file video.
        private static readonly TimeSpan MaxFileNameDiff = TimeSpan.FromMinutes(2);

        private static readonly Regex OutputNamePattern = new(@"^\d{4}_\d{2}_\d{2}_\d{2}_\d{2}_\d{2}(_\d{2})?$");

        private static readonly Lazy<Mat[]> DigitTemplates = new(LoadDigitTemplates);

        public List<string> GetVideos(string input)
        {
            if (Directory.Exists(input))
            {
                return Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories)
                    .Where(f => VideoExtensions.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith('.'))
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();
            }

            if (File.Exists(input))
            {
                return new List<string> { input };
            }

            throw new FileNotFoundException($"Tidak ditemukan: {input}");
        }

        public string GetOutputFolder(string input, string video, string output)
        {
            string root = Directory.Exists(input) ? input : Path.GetDirectoryName(input)!;
            string relative = Path.GetRelativePath(root, video);
            return Path.Combine(output, Path.GetDirectoryName(relative) ?? "", Path.GetFileNameWithoutExtension(video));
        }

        public bool IsExtracted(string outputFolder)
        {
            return File.Exists(Path.Combine(outputFolder, DoneMarker));
        }

        public ExtractResult Extract(string video, string outputFolder, ExtractorOption option)
        {
            double fps;
            int expected;
            using (var capture = OpenVideo(video))
            {
                fps = FFprobe.GetFps(video) ?? capture.Get(VideoCaptureProperties.Fps);
                expected = (int)capture.Get(VideoCaptureProperties.FrameCount);
            }

            if (double.IsNaN(fps) || fps <= 0)
            {
                throw new InvalidOperationException("fps video tidak bisa dibaca");
            }

            Directory.CreateDirectory(outputFolder);
            File.Delete(Path.Combine(outputFolder, DoneMarker));

            // Hapus gambar hasil ekstraksi sebelumnya di folder video ini agar isi folder
            // hanya hasil run ini (hanya file bernama yyyy_MM_dd_HH_mm_ss*.ext).
            foreach (string old in Directory.EnumerateFiles(outputFolder, $"*.{option.Ext}"))
            {
                if (OutputNamePattern.IsMatch(Path.GetFileNameWithoutExtension(old)))
                {
                    File.Delete(old);
                }
            }
            bool fromFileName = TryGetRecordedAt(video, out DateTime start);

            var result = ExtractByFrameStep(video, outputFolder, option, fps, start, fromFileName);

            result.Fps = fps;
            result.ExpectedFrameCount = expected;
            result.RecordedAtFromFileName = fromFileName;

            File.WriteAllText(Path.Combine(outputFolder, DoneMarker), $"{result.Saved}{Environment.NewLine}");
            return result;
        }

        // Tiap round(fps x interval) frame (20 fps x 1 detik = frame ke-0, 20, 40, ...).
        // Nama = jam overlay pada frame itu; bila tidak terbaca, dihitung dari nama file video.
        private static ExtractResult ExtractByFrameStep(string video, string outputFolder, ExtractorOption option, double fps, DateTime start, bool fromFileName)
        {
            using var capture = OpenVideo(video);
            var prms = EncodingParams(option);
            int step = Math.Max(1, (int)Math.Round(fps * option.Interval));

            int index = 0;
            int nextIndex = 0;   // frame berikutnya yang diambil: 0, step, 2*step, ...
            int saved = 0;
            int namedFromOverlay = 0;
            DateTime? lastTime = null;
            var usedNames = new HashSet<string>();
            using var frame = new Mat();

            // Grab() tanpa decode ke Mat -> lebih cepat untuk frame yang dilewati
            while (capture.Grab())
            {
                // Bila frame target gagal di-decode, frame sesudahnya yang dipakai agar tidak ada detik yang hilang.
                if (index >= nextIndex && capture.Retrieve(frame) && !frame.Empty())
                {
                    nextIndex += step;

                    // Jam overlay dipakai bila masuk akal: dekat waktu nama file dan tidak mundur.
                    DateTime? read = ReadOverlay(frame);
                    DateTime time;
                    if (read is DateTime r
                        && (!fromFileName || (r - start).Duration() <= MaxFileNameDiff)
                        && (lastTime is null || r >= lastTime.Value))
                    {
                        time = r;
                        namedFromOverlay++;
                    }
                    else
                    {
                        time = lastTime?.AddSeconds(option.Interval) ?? start.AddSeconds(Math.Floor(index / fps));
                    }
                    lastTime = time;

                    // Dua gambar dengan jam yang sama (kamera drop frame / interval < 1) diberi _01, _02, ...
                    string name = time.ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);
                    string file = name;
                    for (int n = 1; !usedNames.Add(file); n++)
                    {
                        file = $"{name}_{n:D2}";
                    }

                    string path = Path.Combine(outputFolder, $"{file}.{option.Ext}");
                    if (!Cv2.ImWrite(path, frame, prms))
                    {
                        throw new IOException($"gagal menyimpan {path} (disk penuh atau tidak ada izin tulis?)");
                    }
                    saved++;
                }

                index++;
            }

            return new ExtractResult
            {
                FrameCount = index,
                Saved = saved,
                NamedFromOverlay = namedFromOverlay
            };
        }

        // Baca "yyyy-MM-dd HH:mm:ss" dari overlay; null bila ada angka yang tidak yakin.
        private static DateTime? ReadOverlay(Mat frame)
        {
            if (frame.Width != OverlayWidth || frame.Height != OverlayHeight)
            {
                return null;
            }

            int x0 = CellX + CellPitch * DigitCells[0] - CellPad;
            int x1 = CellX + CellPitch * DigitCells[^1] + CellWidth + CellPad;
            var area = new Rect(x0, CellY - CellPad, x1 - x0, CellHeight + 2 * CellPad);

            using var region = new Mat(frame, area);
            using var gray = new Mat();
            Cv2.CvtColor(region, gray, ColorConversionCodes.BGR2GRAY);

            var templates = DigitTemplates.Value;
            var digits = new char[DigitCells.Length];
            using var score = new Mat();

            for (int k = 0; k < DigitCells.Length; k++)
            {
                int x = CellX + CellPitch * DigitCells[k] - CellPad - x0;
                using var cell = new Mat(gray, new Rect(x, 0, CellWidth + 2 * CellPad, CellHeight + 2 * CellPad));

                double best = double.MinValue, second = double.MinValue;
                int bestDigit = -1;
                for (int d = 0; d < templates.Length; d++)
                {
                    Cv2.MatchTemplate(cell, templates[d], score, TemplateMatchModes.CCoeffNormed);
                    Cv2.MinMaxLoc(score, out _, out double max);
                    if (max > best)
                    {
                        second = best;
                        best = max;
                        bestDigit = d;
                    }
                    else if (max > second)
                    {
                        second = max;
                    }
                }

                if (best < MinScore || best - second < MinMargin)
                {
                    return null;
                }

                digits[k] = (char)('0' + bestDigit);
            }

            return DateTime.TryParseExact(new string(digits), "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime parsed) ? parsed : null;
        }

        private static Mat[] LoadDigitTemplates()
        {
            var assembly = typeof(FrameExtractorService).Assembly;
            var templates = new Mat[10];
            for (int d = 0; d < 10; d++)
            {
                using var stream = assembly.GetManifestResourceStream($"OverlayDigits.{d}.png")
                    ?? throw new InvalidOperationException($"Template angka {d} tidak ditemukan");
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                templates[d] = Cv2.ImDecode(memory.ToArray(), ImreadModes.Grayscale);
            }
            return templates;
        }

        private static VideoCapture OpenVideo(string video)
        {
            var capture = new VideoCapture(video);
            if (!capture.IsOpened())
            {
                capture.Dispose();
                throw new InvalidOperationException("OpenCV tidak bisa membuka video");
            }
            return capture;
        }

        private static ImageEncodingParam[] EncodingParams(ExtractorOption option)
        {
            return option.Ext == "jpg"
                ? new[] { new ImageEncodingParam(ImwriteFlags.JpegQuality, option.Quality) }
                : Array.Empty<ImageEncodingParam>();
        }

        // Waktu mulai rekaman dari nama file kamera: 20250811.100002.60222 -> 2025-08-11 10:00:02.
        // Bila tidak cocok, pakai waktu modifikasi file.
        private static bool TryGetRecordedAt(string video, out DateTime recordedAt)
        {
            var match = Regex.Match(Path.GetFileNameWithoutExtension(video),
                @"(?<!\d)(\d{8})[._-]?(\d{6})(?!\d)");

            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value + match.Groups[2].Value, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out recordedAt))
            {
                return true;
            }

            recordedAt = File.GetLastWriteTime(video);
            return false;
        }
    }
}
