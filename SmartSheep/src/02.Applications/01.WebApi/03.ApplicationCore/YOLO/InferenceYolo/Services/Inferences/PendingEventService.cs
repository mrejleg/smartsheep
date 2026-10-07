using System.Globalization;
using System.Text.Json;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Services.Inferences
{
    // Penyimpanan sementara kejadian sebelum di-sync ke database.
    //
    // - Folder per kandang: <Yolo:PendingFolder>/<kode kandang>/. Satu kejadian = satu file JSON, nama file
    //   diawali waktu mulai kejadian (yyyyMMdd_HHmmss_fff) sehingga urutan nama file = urutan waktu saat sync.
    //   Satu kandang bisa punya beberapa SourceVideo; setiap SourceVideo hanya men-sync file miliknya.
    // - Kejadian menyusu yang masih berlangsung ditulis ulang paling cepat tiap WriteInterval.
    // - Bila tidak ada indikasi menyusu selama Yolo:SyncIdleSeconds, file di-sync berurutan lalu dihapus.
    //   Sync tetap dikirim walau tidak ada kejadian (heartbeat) agar API membuat statistik untuk periode
    //   tanpa aktivitas menyusu.
    // - Sync yang gagal membiarkan file tetap ada dan dicoba lagi pada sync berikutnya.
    // - File ditulis atomik (file .tmp lalu rename) agar tidak rusak bila proses berhenti mendadak;
    //   file sisa proses sebelumnya ikut di-sync saat SyncAll(null) dipanggil di awal.
    public class PendingEventService : IPendingEventService
    {
        private const string OfflineBarnCode = "offline";
        private static readonly TimeSpan WriteInterval = TimeSpan.FromSeconds(1);
        private static readonly JsonSerializerOptions JsonOption = new() { WriteIndented = true };

        private readonly ISucklingSyncService syncService;
        private readonly string pendingFolder;
        private readonly string sourceVideoCode;
        private readonly double syncIdleSeconds;
        private readonly Dictionary<Guid, DateTime> lastWrites = new();

        // Acuan jeda: terakhir ada indikasi menyusu atau terakhir sync, sehingga saat tidak ada
        // indikasi menyusu sync terjadi paling sering sekali per Yolo:SyncIdleSeconds
        private double idleFromSeconds;

        // Awal rentang sync berikutnya (frame pertama, lalu batas sync terakhir)
        private DateTime? processedFrom;

        public PendingEventService(ISucklingSyncService syncService, InferenceOption option, AppSetting setting)
        {
            this.syncService = syncService;
            sourceVideoCode = option.SourceVideoCode;
            syncIdleSeconds = double.Parse(setting.YoloSyncIdleSeconds, CultureInfo.InvariantCulture);

            string barnCode = string.IsNullOrWhiteSpace(option.BarnCode) ? OfflineBarnCode : option.BarnCode;
            pendingFolder = Path.Combine(Path.GetFullPath(setting.YoloPendingFolder), ToFileName(barnCode));
            Directory.CreateDirectory(pendingFolder);
        }

        public void Save(SucklingEvent sucklingEvent)
        {
            var now = DateTime.UtcNow;
            if (!sucklingEvent.IsClosed && lastWrites.TryGetValue(sucklingEvent.Id, out var lastWrite) && now - lastWrite < WriteInterval)
            {
                return;
            }

            string path = Path.Combine(pendingFolder, GetFileName(sucklingEvent));
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(sucklingEvent, JsonOption));
            File.Move(temp, path, overwrite: true);

            if (sucklingEvent.IsClosed)
            {
                lastWrites.Remove(sucklingEvent.Id);
            }
            else
            {
                lastWrites[sucklingEvent.Id] = now;
            }
        }

        public void SyncIfIdle(DateTime frameAt, double timeSeconds, bool hasActiveSuckling)
        {
            processedFrom ??= frameAt;
            if (hasActiveSuckling)
            {
                idleFromSeconds = timeSeconds;
                return;
            }

            if (timeSeconds - idleFromSeconds >= syncIdleSeconds)
            {
                SyncAll(frameAt);
                idleFromSeconds = timeSeconds;
            }
        }

        // Sync file pending SourceVideo ini (urut waktu) lalu hapus. processedUntil = waktu frame terakhir;
        // null saat awal proses (file sisa), rentangnya diambil dari kejadian itu sendiri.
        public int SyncAll(DateTime? processedUntil)
        {
            var pendings = Directory.GetFiles(pendingFolder, "*.json")
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .Select(file => (File: file, Event: JsonSerializer.Deserialize<SucklingEvent>(File.ReadAllText(file))!))
                .Where(p => p.Event.SourceVideoCode == sourceVideoCode)
                .ToList();

            if (pendings.Count == 0 && !processedUntil.HasValue)
            {
                return 0;
            }

            foreach (var (_, sucklingEvent) in pendings)
            {
                sucklingEvent.IsClosed = true;
            }

            var until = processedUntil ?? pendings.Max(p => p.Event.EndAt);
            var from = processedUntil.HasValue && processedFrom.HasValue ? processedFrom.Value
                : pendings.Count > 0 ? pendings.Min(p => p.Event.StartAt) : until;

            if (!syncService.Sync(sourceVideoCode, from, until, pendings.Select(p => p.Event).ToList()))
            {
                Console.Error.WriteLine($"[{sourceVideoCode}] Sync gagal, {pendings.Count} kejadian tetap di {pendingFolder} dan dicoba lagi nanti");
                return 0;
            }

            foreach (var (file, _) in pendings)
            {
                File.Delete(file);
            }

            if (processedUntil.HasValue)
            {
                processedFrom = until;
            }

            return pendings.Count;
        }

        private static string GetFileName(SucklingEvent sucklingEvent)
        {
            return $"{sucklingEvent.StartAt:yyyyMMdd_HHmmss_fff}_{ToFileName(sucklingEvent.SourceVideoCode)}_{sucklingEvent.LambTrackId}_{sucklingEvent.EventType}.json";
        }

        private static string ToFileName(string value)
        {
            return string.Concat(value.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        }
    }
}
