using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Services.Inferences
{
    // Statistik pemantauan per periode (proposal Tabel 6):
    // frekuensi pendekatan, jumlah dan durasi menyusu (Persamaan 11), pendekatan gagal,
    // jeda terpanjang tanpa menyusu, serta kategori waktu siang (06.00-17.59) / malam (18.00-05.59).
    public class SucklingStatisticService : ISucklingStatisticService
    {
        public SucklingStatistic Calculate(string sourceVideoCode, List<SucklingEvent> sucklingEvents, DateTime from, DateTime to)
        {
            var sucklings = sucklingEvents.Where(e => e.EventType == SucklingEvent.Suckling).OrderBy(e => e.StartAt).ToList();

            // Jeda dihitung dari awal periode, antar kejadian, sampai akhir periode
            double longestGap = 0;
            var cursor = from;
            foreach (var suckling in sucklings)
            {
                longestGap = Math.Max(longestGap, (suckling.StartAt - cursor).TotalSeconds);
                if (suckling.EndAt > cursor)
                {
                    cursor = suckling.EndAt;
                }
            }
            longestGap = Math.Max(longestGap, (to - cursor).TotalSeconds);

            return new SucklingStatistic
            {
                SourceVideoCode = sourceVideoCode,
                ActivityFrom = from,
                ActivityTo = to,
                HourNumber = from.Hour,
                TimeCategory = from.Hour is >= 6 and < 18 ? "Siang" : "Malam",
                ObservationSeconds = Math.Round((to - from).TotalSeconds, 3),
                TotalApproach = sucklingEvents.Count(e => e.EventType == SucklingEvent.Approach),
                TotalFrequency = sucklings.Count,
                TotalDurationSeconds = Math.Round(sucklings.Sum(e => e.DurationSeconds), 3),
                TotalFailedAttempt = sucklingEvents.Count(e => e.EventType == SucklingEvent.FailedAttempt),
                LongestGapSeconds = Math.Round(longestGap, 3)
            };
        }
    }
}
