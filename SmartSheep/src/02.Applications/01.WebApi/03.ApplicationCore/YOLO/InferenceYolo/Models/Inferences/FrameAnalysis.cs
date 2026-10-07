using OpenCvSharp;

namespace InferenceYolo.Models.Inferences
{
    public class FrameAnalysis
    {
        public double TimeSeconds { get; set; }
        public DateTime FrameAt { get; set; }
        public List<TrackedObject> Objects { get; set; } = new();
        public List<LambStatus> Lambs { get; set; } = new();

        // Per track induk: zona ambing (kedua belahan badan sepanjang sumbu, diperlebar ke samping badan)
        public Dictionary<int, Rect> UdderZones { get; set; } = new();

        // Kejadian menyusu yang sedang berlangsung (sudah melewati durasi minimal) dan kejadian yang selesai di frame ini
        public List<SucklingEvent> ActiveEvents { get; set; } = new();
        public List<SucklingEvent> ClosedEvents { get; set; } = new();
    }
}
