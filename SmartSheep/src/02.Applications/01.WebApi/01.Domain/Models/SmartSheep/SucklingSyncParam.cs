namespace Api.Domain.Models.SmartSheep
{
    // Kiriman sync dari InferenceYolo untuk satu SourceVideo.
    public class SucklingSyncParam
    {
        public string SourceVideoCode { get; set; } = string.Empty;

        // Rentang waktu video yang diproses worker sejak sync sebelumnya. Statistik dihitung ulang untuk
        // periode dalam rentang ini (termasuk periode tanpa kejadian); periode yang berakhir sebelum
        // ProcessedUntil dianggap selesai dan dinilai RequiresReminder-nya.
        public DateTime ProcessedFrom { get; set; }
        public DateTime ProcessedUntil { get; set; }

        public List<SucklingSyncEventParam> Events { get; set; } = new();
    }

    public class SucklingSyncEventParam
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int LambTrackId { get; set; }
        public int EweTrackId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public double DurationSeconds { get; set; }
        public int FrameCount { get; set; }
    }
}
