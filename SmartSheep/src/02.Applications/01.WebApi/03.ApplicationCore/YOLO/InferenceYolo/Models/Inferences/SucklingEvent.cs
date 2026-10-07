namespace InferenceYolo.Models.Inferences
{
    // Satu kejadian anak terhadap induk; disimpan sebagai file JSON pending sampai di-sync.
    public class SucklingEvent
    {
        public const string Suckling = "Suckling";
        public const string FailedAttempt = "FailedAttempt";
        public const string Approach = "Approach";

        public Guid Id { get; set; } = Guid.NewGuid();     // tetap sama walau di-sync ulang (idempotent)
        public string EventType { get; set; } = Suckling;
        public string SourceVideoCode { get; set; } = "";
        public int LambTrackId { get; set; }
        public int EweTrackId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public double DurationSeconds { get; set; }
        public int FrameCount { get; set; }
        public bool IsClosed { get; set; }
    }
}
