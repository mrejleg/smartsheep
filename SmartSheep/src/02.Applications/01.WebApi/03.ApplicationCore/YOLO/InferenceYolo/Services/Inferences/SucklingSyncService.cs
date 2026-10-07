using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Services.Inferences
{
    // Klien API SmartSheep untuk sync kejadian dan mengambil sumber video.
    //
    // - Token diminta ke Auth/Token dengan Yolo:ClientId dan Yolo:ClientSecret, disimpan sampai kedaluwarsa,
    //   dan diminta ulang bila API menjawab 401.
    // - Sync dikirim per SourceVideo ke SucklingActivity/Sync bersama rentang waktu video yang diproses sejak
    //   sync sebelumnya (ProcessedFrom-ProcessedUntil) agar API bisa menutup periode statistik yang sudah selesai.
    // - --offline: kejadian ditulis ke <output>/synced_events.csv (uji/kalibrasi tanpa API).
    public sealed class SucklingSyncService : ISucklingSyncService, IDisposable
    {
        private const string CsvHeader = "id,event_type,source_video_code,lamb_track_id,ewe_track_id,start_at,end_at,duration_seconds,frame_count";
        private const string WorkerUsername = "InferenceYolo";

        private readonly bool isOffline;
        private readonly string csvPath;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly HttpClient http;
        private string token;
        private DateTime tokenValidTo;

        public SucklingSyncService(InferenceOption option, AppSetting setting)
        {
            isOffline = option.IsOffline;
            csvPath = Path.Combine(option.Output, "synced_events.csv");
            clientId = setting.YoloClientId;
            clientSecret = setting.YoloClientSecret;

            string apiUrl = setting.YoloApiUrl.EndsWith('/') ? setting.YoloApiUrl : setting.YoloApiUrl + "/";
            http = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromSeconds(30) };
        }

        public bool Sync(string sourceVideoCode, DateTime processedFrom, DateTime processedUntil, List<SucklingEvent> sucklingEvents)
        {
            return isOffline ? WriteCsv(sucklingEvents) : Post(sourceVideoCode, processedFrom, processedUntil, sucklingEvents);
        }

        // SourceVideo online (aktif dan punya kandang) untuk worker background
        public List<SyncSource> GetSources()
        {
            using var response = Send(() => new HttpRequestMessage(HttpMethod.Get, "SucklingActivity/SyncSources"));
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Daftar SourceVideo tidak bisa diambil dari API ({(int)response.StatusCode})");
            }

            return ReadData(response).EnumerateArray().Select(ToSyncSource).ToList();
        }

        public SyncSource GetSource(string sourceVideoCode)
        {
            using var response = Send(() => new HttpRequestMessage(HttpMethod.Get, $"SucklingActivity/SyncSource/{Uri.EscapeDataString(sourceVideoCode)}"));
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"SourceVideo {sourceVideoCode} tidak ditemukan di API ({(int)response.StatusCode})");
            }

            return ToSyncSource(ReadData(response));
        }

        // Username/password kamera disisipkan ke URL bila URL belum memuatnya
        private static SyncSource ToSyncSource(JsonElement data)
        {
            string sourceVideoUrl = data.GetProperty("SourceVideoUrl").GetString() ?? "";
            string username = data.GetProperty("Username").GetString();
            if (!string.IsNullOrEmpty(username) && Uri.TryCreate(sourceVideoUrl, UriKind.Absolute, out var uri) && string.IsNullOrEmpty(uri.UserInfo))
            {
                var url = new UriBuilder(uri)
                {
                    UserName = Uri.EscapeDataString(username),
                    Password = Uri.EscapeDataString(data.GetProperty("Password").GetString() ?? "")
                };
                sourceVideoUrl = url.Uri.AbsoluteUri;
            }

            return new SyncSource
            {
                Code = data.GetProperty("Code").GetString() ?? "",
                BarnCode = data.GetProperty("BarnCode").GetString() ?? "",
                SourceVideoUrl = sourceVideoUrl
            };
        }

        private bool Post(string sourceVideoCode, DateTime processedFrom, DateTime processedUntil, List<SucklingEvent> sucklingEvents)
        {
            var body = new
            {
                SourceVideoCode = sourceVideoCode,
                ProcessedFrom = processedFrom,
                ProcessedUntil = processedUntil,
                Events = sucklingEvents.Select(e => new
                {
                    e.Id,
                    e.EventType,
                    e.LambTrackId,
                    e.EweTrackId,
                    e.StartAt,
                    e.EndAt,
                    e.DurationSeconds,
                    e.FrameCount
                })
            };

            try
            {
                using var response = Send(() => new HttpRequestMessage(HttpMethod.Post, "SucklingActivity/Sync") { Content = JsonContent.Create(body) });
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                Console.Error.WriteLine($"Sync {sourceVideoCode}: API menjawab {(int)response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                return false;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                Console.Error.WriteLine($"Sync {sourceVideoCode}: {ex.Message}");
                return false;
            }
        }

        // Kirim request dengan token; bila 401, token diminta ulang lalu dikirim sekali lagi
        private HttpResponseMessage Send(Func<HttpRequestMessage> createRequest)
        {
            for (int attempt = 0; ; attempt++)
            {
                using var request = createRequest();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetToken());
                var response = http.Send(request);
                if (response.StatusCode != HttpStatusCode.Unauthorized || attempt > 0)
                {
                    return response;
                }

                response.Dispose();
                token = null;
            }
        }

        private string GetToken()
        {
            if (token != null && DateTime.Now < tokenValidTo.AddMinutes(-5))
            {
                return token;
            }

            using var response = http.PostAsJsonAsync("Auth/Token", new { ClientId = clientId, ClientSecret = clientSecret, Username = WorkerUsername }).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token API ditolak ({(int)response.StatusCode}); periksa Yolo:ClientId dan Yolo:ClientSecret");
            }

            var data = ReadData(response);
            token = data.GetProperty("Token").GetString();
            tokenValidTo = data.GetProperty("ValidTo").GetDateTime();
            return token;
        }

        private static JsonElement ReadData(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
            return document.RootElement.GetProperty("Data").Clone();
        }

        private bool WriteCsv(List<SucklingEvent> sucklingEvents)
        {
            try
            {
                bool isNew = !File.Exists(csvPath);
                using var writer = new StreamWriter(csvPath, append: true);
                if (isNew)
                {
                    writer.WriteLine(CsvHeader);
                }

                foreach (var e in sucklingEvents)
                {
                    writer.WriteLine(string.Join(',', e.Id, e.EventType, e.SourceVideoCode, e.LambTrackId, e.EweTrackId,
                        e.StartAt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                        e.EndAt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                        e.DurationSeconds.ToString("0.000", CultureInfo.InvariantCulture), e.FrameCount));
                }

                return true;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Sync: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            http.Dispose();
        }
    }
}
