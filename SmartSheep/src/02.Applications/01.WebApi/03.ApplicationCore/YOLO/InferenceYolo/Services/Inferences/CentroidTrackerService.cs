using System.Globalization;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using OpenCvSharp;

namespace InferenceYolo.Services.Inferences
{
    // Centroid tracking (proposal 3.3.1, Persamaan 8 dan 9).
    //
    // - Centroid = ((x1 + x2) / 2, (y1 + y2) / 2), jarak = Euclidean.
    // - Pasangan dicari per kelas (anak dengan anak, induk dengan induk); pasangan dengan jarak
    //   terkecil dipilih lebih dulu dan mempertahankan track ID. Tidak ada jarak maksimum karena
    //   objek berada di dalam kandang.
    // - Deteksi tanpa pasangan menjadi track baru dengan ID sementara. Track sementara yang hilang
    //   sebelum terkonfirmasi dibuang (biasanya deteksi palsu sesaat); track terkonfirmasi tetap
    //   disimpan dengan posisi terakhir dan dipasangkan lagi saat objek muncul kembali.
    public class CentroidTrackerService : ICentroidTrackerService
    {
        // Jumlah frame berturut-turut agar track sementara menjadi tetap (Yolo:TrackConfirmFrames)
        private readonly int confirmFrames;

        private readonly List<TrackedObject> tracks = new();
        private int nextTrackId = 1;

        public CentroidTrackerService(AppSetting setting)
        {
            confirmFrames = int.Parse(setting.YoloTrackConfirmFrames, CultureInfo.InvariantCulture);
        }

        public List<TrackedObject> Update(List<Detection> detections)
        {
            foreach (var track in tracks)
            {
                track.IsVisible = false;
            }

            foreach (var group in detections.GroupBy(d => d.ClassId))
            {
                Match(group.Key, group.ToList());
            }

            // Track sementara yang tidak terdeteksi lagi dibuang
            tracks.RemoveAll(t => !t.IsVisible && !t.IsConfirmed);

            return tracks;
        }

        private void Match(int classId, List<Detection> detections)
        {
            var candidates = tracks.Where(t => t.ClassId == classId).ToList();

            var pairs = new List<(float Distance, int Track, int Detection)>(candidates.Count * detections.Count);
            for (int t = 0; t < candidates.Count; t++)
            {
                for (int d = 0; d < detections.Count; d++)
                {
                    pairs.Add((Distance(candidates[t].Centroid, GetCentroid(detections[d].Box)), t, d));
                }
            }
            pairs.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            var usedTracks = new bool[candidates.Count];
            var usedDetections = new bool[detections.Count];
            foreach (var (_, t, d) in pairs)
            {
                if (usedTracks[t] || usedDetections[d])
                {
                    continue;
                }

                usedTracks[t] = true;
                usedDetections[d] = true;
                Apply(candidates[t], detections[d]);
            }

            for (int d = 0; d < detections.Count; d++)
            {
                if (!usedDetections[d])
                {
                    var track = new TrackedObject { TrackId = nextTrackId++, ClassId = classId };
                    Apply(track, detections[d]);
                    tracks.Add(track);
                }
            }
        }

        private void Apply(TrackedObject track, Detection detection)
        {
            track.Label = detection.Label;
            track.Confidence = detection.Confidence;
            track.Box = detection.Box;
            track.Centroid = GetCentroid(detection.Box);
            track.IsVisible = true;
            track.Hits++;
            track.IsConfirmed |= track.Hits >= confirmFrames;
        }

        public static Point2f GetCentroid(Rect box)
        {
            return new Point2f(box.X + box.Width / 2f, box.Y + box.Height / 2f);
        }

        public static float Distance(Point2f a, Point2f b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}
