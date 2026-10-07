namespace InferenceYolo.Models.Inferences
{
    // Hasil pengecekan aturan menyusu untuk satu anak pada satu frame.
    public class LambStatus
    {
        public int LambTrackId { get; set; }
        public int? EweTrackId { get; set; }
        public bool IsVisible { get; set; }
        public bool IsMoving { get; set; }
        public bool IsOverlap { get; set; }
        public bool IsInUdderZone { get; set; }
        public bool IsStanding { get; set; }
        public bool IsShrunk { get; set; }
        public bool IsStayPoint { get; set; }

        // true setelah stay point melewati durasi minimal menyusu
        public bool IsSuckling { get; set; }
        public double StayPointSeconds { get; set; }

        public float Ratio { get; set; }
        public float Area { get; set; }
        public float? BaselineRatio { get; set; }
        public float? BaselineArea { get; set; }
    }
}
