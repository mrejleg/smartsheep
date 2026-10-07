using System.Text;
using System.Text.RegularExpressions;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Services.MasterDatas
{
    // Sumber tampilan streaming SourceVideo di Web.
    //
    // - Link YouTube diputar lewat iframe embed (GetEmbedUrl).
    // - Sumber lain (RTSP/HTTP stream, file video, atau folder rekaman CCTV seperti Google Drive) dikirim sebagai
    //   MJPEG (multipart/x-mixed-replace) karena browser tidak bisa memutar RTSP maupun video HEVC .avi.
    // - Folder rekaman (tahun/bulan/hari/jam): langsung ke subfolder terbaru (nama terbesar) tanpa memindai
    //   seluruh folder.
    public class SourceVideoStreamService : ISourceVideoStreamService
    {
        private static readonly HashSet<string> VideoExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".avi", ".mp4", ".mov", ".mkv", ".m4v" };
        private static readonly Regex YouTubeId = new(@"(?:youtube\.com/(?:watch\?v=|embed/|live/)|youtu\.be/)([\w-]{11})");

        public string GetEmbedUrl(string sourceVideoUrl)
        {
            var match = YouTubeId.Match(sourceVideoUrl ?? "");
            return match.Success ? $"https://www.youtube.com/embed/{match.Groups[1].Value}?autoplay=1&mute=1" : null;
        }

        // Folder: subfolder terbaru yang berisi video; stream: URL dengan username/password kamera bila belum ada
        public string GetInput(SourceVideo sourceVideo)
        {
            string url = sourceVideo.SourceVideoUrl;
            if (Directory.Exists(url))
            {
                return GetLatestFolder(url);
            }

            if (!string.IsNullOrEmpty(sourceVideo.Username) && url.Contains("://", StringComparison.Ordinal) &&
                Uri.TryCreate(url, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.UserInfo))
            {
                url = new UriBuilder(uri)
                {
                    UserName = Uri.EscapeDataString(sourceVideo.Username),
                    Password = Uri.EscapeDataString(sourceVideo.Password ?? "")
                }.Uri.AbsoluteUri;
            }

            return url;
        }

        public async Task WriteFrameAsync(Stream output, byte[] jpeg, CancellationToken cancellationToken)
        {
            var header = Encoding.ASCII.GetBytes($"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {jpeg.Length}\r\n\r\n");
            await output.WriteAsync(header, cancellationToken);
            await output.WriteAsync(jpeg, cancellationToken);
            await output.WriteAsync("\r\n"u8.ToArray(), cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        // Turun ke subfolder dengan nama terbesar sampai menemukan folder yang berisi video
        private static string GetLatestFolder(string folder)
        {
            while (!Directory.EnumerateFiles(folder).Any(f => VideoExtensions.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith('.')))
            {
                var latest = Directory.EnumerateDirectories(folder)
                    .Where(d => !Path.GetFileName(d).StartsWith('.'))
                    .OrderBy(d => d, StringComparer.Ordinal)
                    .LastOrDefault();
                if (latest == null)
                {
                    break;
                }

                folder = latest;
            }

            return folder;
        }
    }
}
