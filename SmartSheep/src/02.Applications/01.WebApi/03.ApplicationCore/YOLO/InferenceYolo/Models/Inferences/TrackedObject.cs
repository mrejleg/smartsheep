using OpenCvSharp;

namespace InferenceYolo.Models.Inferences
{
    public class TrackedObject
    {
        public int TrackId { get; set; }
        public int ClassId { get; set; }
        public string Label { get; set; } = "";
        public float Confidence { get; set; }
        public Rect Box { get; set; }
        public Point2f Centroid { get; set; }

        // Jumlah frame objek terdeteksi; track masih sementara sampai mencapai batas konfirmasi.
        public int Hits { get; set; }
        public bool IsConfirmed { get; set; }

        // false = tidak terdeteksi pada frame ini, posisi terakhir tetap disimpan.
        public bool IsVisible { get; set; }
    }
}
