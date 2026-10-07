using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using OpenCvSharp;

namespace InferenceYolo.Services.Inferences
{
    // Sumber frame: file video, folder rekaman, atau live streaming (input berisi "://").
    //
    // - Frame yang diproses dibatasi Yolo:MaximumProcessFps (0 = semua frame) agar ringan; aturan menyusu
    //   berbasis detik sehingga tidak bergantung pada fps.
    // - File/folder : dibaca berurutan; frame di antara frame yang diproses dilewati dengan Grab (tanpa
    //                 konversi warna). Folder diproses sebagai satu rekaman
    //                 bersambung (urut nama file = urut waktu) agar tracker dan baseline anak tidak
    //                 ter-reset di setiap klip. Waktu frame = waktu mulai klip (dari nama file kamera)
    //                 + nomor frame / fps klip; fps dibaca per klip (CCTV bisa 10 atau 20 fps).
    // - Streaming   : dibaca di thread terpisah dan hanya frame terbaru yang disimpan, sehingga bila
    //                 inferensi lebih lambat dari kamera frame lama dibuang dan proses tetap real time.
    //                 Waktu frame = waktu saat frame diterima; waktu mulai = saat stream dibuka.
    //                 Bila koneksi putus atau tidak ada frame selama StreamTimeoutMs, stream disambung
    //                 ulang terus dengan jeda bertahap (2, 4, 8 ... maks 30 detik) dan ReconnectCount naik.
    public sealed class VideoSourceService : IVideoSourceService
    {
        private static readonly HashSet<string> VideoExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".avi", ".mp4", ".mov", ".mkv", ".m4v" };

        // Batas tunggu buka/baca stream agar kamera yang macet tanpa error tetap terdeteksi putus
        private const int StreamTimeoutMs = 10000;
        private static readonly TimeSpan MinReconnectDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

        private readonly double maximumProcessFps;
        private VideoCapture capture;

        // File/folder
        private readonly Queue<string> videos = new();
        private int frameIndex;
        private int frameStride = 1;
        private double clipOffsetSeconds;
        private double lastSeconds = -1;

        // Streaming
        private string streamUrl;
        private readonly object sync = new();
        private readonly Stopwatch clock = new();
        private Thread reader;
        private Mat grabbed = new();
        private Mat latest = new();
        private double latestSeconds;
        private bool hasNewFrame;
        private bool isStopped;

        public bool IsStream { get; private set; }
        public int ReconnectCount { get; private set; }
        public int VideoCount { get; private set; }
        public double Fps { get; private set; }
        public int FrameCount { get; private set; }
        public Size FrameSize { get; private set; }
        public DateTime StartedAt { get; private set; }

        public VideoSourceService(AppSetting setting)
        {
            maximumProcessFps = double.Parse(setting.YoloMaximumProcessFps, CultureInfo.InvariantCulture);
        }

        public void Open(string input)
        {
            IsStream = input.Contains("://", StringComparison.Ordinal);
            if (IsStream)
            {
                streamUrl = input;
                capture = OpenStream(input) ?? throw new InvalidOperationException($"OpenCV tidak bisa membuka {input}");

                Fps = capture.Fps;
                FrameSize = new Size(capture.FrameWidth, capture.FrameHeight);
                StartedAt = DateTime.Now;
                clock.Start();
                reader = new Thread(ReadLoop) { IsBackground = true, Name = "VideoSourceReader" };
                reader.Start();
                return;
            }

            foreach (string video in GetVideos(input))
            {
                videos.Enqueue(video);
            }

            VideoCount = videos.Count;
            StartedAt = GetRecordedAt(videos.Peek());
            OpenNextVideo();
            FrameCount = VideoCount == 1 ? capture.FrameCount : 0;
        }

        public bool Read(Mat frame, out double timeSeconds)
        {
            return IsStream ? ReadStream(frame, out timeSeconds) : ReadVideo(frame, out timeSeconds);
        }

        // Hentikan pembacaan (Ctrl+C); Read yang sedang menunggu frame langsung mengembalikan false
        public void Stop()
        {
            lock (sync)
            {
                isStopped = true;
                Monitor.PulseAll(sync);
            }
        }

        private bool ReadVideo(Mat frame, out double timeSeconds)
        {
            while (!isStopped)
            {
                if (capture.Read(frame) && !frame.Empty())
                {
                    timeSeconds = clipOffsetSeconds + frameIndex / Fps;
                    frameIndex++;
                    for (int skip = 1; skip < frameStride && capture.Grab(); skip++)
                    {
                        frameIndex++;
                    }

                    lastSeconds = timeSeconds;
                    return true;
                }

                if (videos.Count == 0)
                {
                    break;
                }

                OpenNextVideo();
            }

            timeSeconds = lastSeconds;
            return false;
        }

        private void OpenNextVideo()
        {
            string video = videos.Dequeue();
            capture?.Dispose();
            capture = new VideoCapture(video);
            if (!capture.IsOpened() || capture.Fps <= 0)
            {
                throw new InvalidOperationException($"OpenCV tidak bisa membuka atau membaca fps: {video}");
            }

            Fps = capture.Fps;
            FrameSize = new Size(capture.FrameWidth, capture.FrameHeight);
            frameIndex = 0;
            frameStride = maximumProcessFps > 0 && Fps > maximumProcessFps ? (int)Math.Round(Fps / maximumProcessFps) : 1;

            // Waktu klip berikutnya tidak boleh mundur dari frame terakhir klip sebelumnya
            clipOffsetSeconds = Math.Max((GetRecordedAt(video) - StartedAt).TotalSeconds, lastSeconds + 1 / Fps);
        }

        private bool ReadStream(Mat frame, out double timeSeconds)
        {
            double minInterval = maximumProcessFps > 0 ? 1 / maximumProcessFps : 0;
            lock (sync)
            {
                while (!isStopped && (!hasNewFrame || latestSeconds - lastSeconds < minInterval))
                {
                    Monitor.Wait(sync);
                }

                timeSeconds = latestSeconds;
                if (!hasNewFrame)
                {
                    return false;
                }

                latest.CopyTo(frame);
                hasNewFrame = false;
                lastSeconds = latestSeconds;
                return true;
            }
        }

        private void ReadLoop()
        {
            while (capture != null)
            {
                if (!capture.Read(grabbed) || grabbed.Empty())
                {
                    if (isStopped)
                    {
                        return;
                    }

                    Reconnect();
                    continue;
                }

                double seconds = clock.Elapsed.TotalSeconds;
                lock (sync)
                {
                    if (isStopped)
                    {
                        return;
                    }

                    // Tukar buffer: frame baru jadi yang terbaru, frame lama yang belum diproses dibuang
                    (latest, grabbed) = (grabbed, latest);
                    latestSeconds = seconds;
                    hasNewFrame = true;
                    Monitor.PulseAll(sync);
                }
            }
        }

        // Sambung ulang sampai berhasil atau dihentikan; capture = null bila dihentikan
        private void Reconnect()
        {
            capture.Dispose();
            capture = null;

            var delay = MinReconnectDelay;
            while (true)
            {
                Console.Error.WriteLine($"Stream terputus, sambung ulang dalam {delay.TotalSeconds:0} detik ...");
                lock (sync)
                {
                    if (!isStopped)
                    {
                        Monitor.Wait(sync, delay);
                    }

                    if (isStopped)
                    {
                        return;
                    }
                }

                var reopened = OpenStream(streamUrl);
                if (reopened != null)
                {
                    lock (sync)
                    {
                        capture = reopened;
                        ReconnectCount++;
                    }
                    Console.WriteLine("Stream tersambung kembali");
                    return;
                }

                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxReconnectDelay.Ticks));
            }
        }

        private static VideoCapture OpenStream(string url)
        {
            var stream = new VideoCapture(url, VideoCaptureAPIs.FFMPEG, new[]
            {
                (int)VideoCaptureProperties.OpenTimeoutMsec, StreamTimeoutMs,
                (int)VideoCaptureProperties.ReadTimeoutMsec, StreamTimeoutMs
            });

            if (stream.IsOpened())
            {
                return stream;
            }

            stream.Dispose();
            return null;
        }

        private static List<string> GetVideos(string input)
        {
            if (Directory.Exists(input))
            {
                var found = Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories)
                    .Where(f => VideoExtensions.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith('.'))
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();

                return found.Count > 0 ? found : throw new InvalidOperationException($"Tidak ada file video di {input}");
            }

            return File.Exists(input) ? new List<string> { input } : throw new InvalidOperationException($"Tidak ditemukan: {input}");
        }

        // Waktu mulai rekaman dari nama file kamera: 20250808.100001.60637 -> 2025-08-08 10:00:01.
        // Bila tidak cocok, pakai waktu modifikasi file.
        private static DateTime GetRecordedAt(string video)
        {
            var match = Regex.Match(Path.GetFileNameWithoutExtension(video), @"(?<!\d)(\d{8})[._-]?(\d{6})(?!\d)");
            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value + match.Groups[2].Value, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var recordedAt))
            {
                return recordedAt;
            }

            return File.GetLastWriteTime(video);
        }

        public void Dispose()
        {
            Stop();
            reader?.Join();
            capture?.Dispose();
            grabbed.Dispose();
            latest.Dispose();
        }
    }
}
