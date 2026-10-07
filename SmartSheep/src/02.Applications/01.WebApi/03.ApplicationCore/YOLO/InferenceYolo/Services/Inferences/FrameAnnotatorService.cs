using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models.Inferences;
using OpenCvSharp;

namespace InferenceYolo.Services.Inferences
{
    // Gambar hasil deteksi dan pelacakan pada frame (untuk video output): bbox, centroid, dan label track.
    // Zona ambing dan status aturan menyusu hanya dihitung di belakang (SucklingAnalyzerService) dan tidak digambar.
    public class FrameAnnotatorService : IFrameAnnotatorService
    {
        private const string LambLabel = "anak";

        // Warna BGR
        private static readonly Scalar EweColor = new(80, 200, 0);
        private static readonly Scalar LambColor = new(0, 165, 255);

        private const HersheyFonts Font = HersheyFonts.HersheySimplex;

        public void Draw(Mat frame, FrameAnalysis analysis)
        {
            foreach (var track in analysis.Objects.Where(o => o.IsConfirmed && o.IsVisible))
            {
                var color = track.Label == LambLabel ? LambColor : EweColor;
                Cv2.Rectangle(frame, track.Box, color, 3);
                Cv2.Circle(frame, (Point)track.Centroid, 6, color, -1);
                DrawLabel(frame, $"{track.Label} #{track.TrackId}", track.Box.TopLeft, color);
            }
        }

        // Label ditempel di atas titik anchor
        private static void DrawLabel(Mat frame, string text, Point anchor, Scalar color)
        {
            var size = Cv2.GetTextSize(text, Font, 0.8, 2, out int baseline);
            int height = size.Height + baseline + 6;
            int top = Math.Max(anchor.Y - height, 0);
            Cv2.Rectangle(frame, new Rect(anchor.X, top, size.Width + 8, height), color, -1);
            Cv2.PutText(frame, text, new Point(anchor.X + 4, top + size.Height + 2), Font, 0.8, Scalar.Black, 2);
        }
    }
}
