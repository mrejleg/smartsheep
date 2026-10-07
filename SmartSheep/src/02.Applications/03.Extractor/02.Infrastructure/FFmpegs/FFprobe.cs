using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Extractor.Infrastructure.FFmpegs
{
    public static class FFprobe
    {
        // Batas waktu: file Google Drive yang belum terunduh bisa membuat ffprobe menunggu lama.
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

        // FPS asli: nb_frames / duration (fallback avg_frame_rate).
        // Header AVI kamera menulis r_frame_rate=15 padahal video sebenarnya 20 fps.
        public static double? GetFps(string video)
        {
            string json;
            try
            {
                var psi = new ProcessStartInfo("ffprobe")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                foreach (var a in new[] { "-v", "error", "-select_streams", "v:0",
                                          "-show_entries", "stream=nb_frames,duration,avg_frame_rate",
                                          "-of", "json", video })
                {
                    psi.ArgumentList.Add(a);
                }

                using var process = Process.Start(psi)!;
                var stdout = process.StandardOutput.ReadToEndAsync();
                var stderr = process.StandardError.ReadToEndAsync(); // dibaca paralel agar tidak deadlock

                if (!process.WaitForExit(Timeout))
                {
                    process.Kill(entireProcessTree: true);
                    return null;
                }

                json = stdout.Result;
                stderr.Wait();
                if (process.ExitCode != 0)
                {
                    return null;
                }
            }
            catch (System.ComponentModel.Win32Exception) // ffprobe tidak terpasang
            {
                return null;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException)
            {
                return null;
            }

            using var _ = doc;
            if (!doc.RootElement.TryGetProperty("streams", out var streams) || streams.GetArrayLength() == 0)
            {
                return null;
            }

            var s = streams[0];
            if (TryGetDouble(s, "nb_frames", out double frames) && TryGetDouble(s, "duration", out double duration) && duration > 0)
            {
                return frames / duration;
            }

            if (s.TryGetProperty("avg_frame_rate", out var rate) && rate.GetString() is string r)
            {
                var parts = r.Split('/');
                if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
                {
                    double den = parts.Length > 1 && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 1;
                    if (den > 0 && num > 0)
                    {
                        return num / den;
                    }
                }
            }

            return null;
        }

        private static bool TryGetDouble(JsonElement element, string name, out double value)
        {
            value = 0;
            return element.TryGetProperty(name, out var prop)
                && prop.GetString() is string text
                && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
