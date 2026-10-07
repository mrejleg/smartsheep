namespace Web.Domain.Models.MasterDatas
{
    // Panel live halaman streaming SourceVideo.
    // - Status, FrameAt, EweCount, LambCount, Fps: dari preview inferensi video (hanya tampilan).
    // - Selebihnya dibaca dari file JSON pending worker (<Yolo:PendingFolder>/<kode kandang>/) untuk SourceVideo ini:
    //   kejadian yang masih berjalan dan yang sudah selesai tetapi belum di-sync ke API.
    public class SourceVideoInferenceStatistic
    {
        public string Status { get; set; } = "Memulai";
        public DateTime? FrameAt { get; set; }
        public int EweCount { get; set; }
        public int LambCount { get; set; }
        public double Fps { get; set; }

        public DateTime? WorkerUpdatedAt { get; set; }
        public DateTime? WorkerFrom { get; set; }
        public DateTime? WorkerUntil { get; set; }
        public List<SourceVideoInferenceSuckling> SucklingNow { get; set; } = new();
        public int TotalApproach { get; set; }
        public int TotalFrequency { get; set; }
        public double TotalDurationSeconds { get; set; }
        public int TotalFailedAttempt { get; set; }
        public double LongestGapSeconds { get; set; }
    }

    public class SourceVideoInferenceSuckling
    {
        public int LambTrackId { get; set; }
        public int EweTrackId { get; set; }
        public DateTime StartAt { get; set; }
        public double Seconds { get; set; }
    }
}
