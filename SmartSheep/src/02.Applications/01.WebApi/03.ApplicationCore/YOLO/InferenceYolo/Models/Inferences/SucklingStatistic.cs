namespace InferenceYolo.Models.Inferences
{
    // Indikator aktivitas menyusu per periode pengamatan (proposal Tabel 6 dan 3.3.3).
    public class SucklingStatistic
    {
        public string SourceVideoCode { get; set; } = "";
        public DateTime ActivityFrom { get; set; }
        public DateTime ActivityTo { get; set; }
        public int HourNumber { get; set; }
        public string TimeCategory { get; set; } = "";       // Siang (06.00-17.59) atau Malam (18.00-05.59)
        public double ObservationSeconds { get; set; }
        public int TotalApproach { get; set; }
        public int TotalFrequency { get; set; }
        public double TotalDurationSeconds { get; set; }
        public int TotalFailedAttempt { get; set; }
        public double LongestGapSeconds { get; set; }
    }
}
