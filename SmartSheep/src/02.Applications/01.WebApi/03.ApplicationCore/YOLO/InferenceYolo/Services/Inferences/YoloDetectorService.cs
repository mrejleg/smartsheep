using System.Globalization;
using System.Text.Json;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace InferenceYolo.Services.Inferences
{
    // Inferensi model YOLO (hasil ExtractorToDotnet/export.py) dengan ONNX Runtime.
    //
    // Input  : images  [1, 3, S, S] float, RGB, 0-1, letterbox (padding abu-abu 114).
    // Output : output0 [1, 4 + kelas, N] -> per kandidat: cx, cy, w, h, skor tiap kelas
    //          (koordinat piksel input S x S). Model tidak end2end, jadi NMS dilakukan di sini.
    public sealed class YoloDetectorService : IYoloDetectorService
    {
        private const byte PadValue = 114;
        private const string EweLabel = "induk";
        private const string LambLabel = "anak";

        private readonly InferenceSession session;
        private readonly int[] inputShape;
        private readonly int classCount;
        private readonly int candidateCount;
        private readonly float confidenceThreshold;

        // IoU NMS (Yolo:NmsIouThreshold; default Ultralytics predict 0.7)
        private readonly float nmsIouThreshold;

        // Kotak yang sebagian besar luasnya (Yolo:NmsContainmentThreshold) berada di dalam kotak sekelas yang lebih besar
        // dianggap potongan objek yang sama (IoU-nya kecil karena ukurannya beda jauh)
        private readonly float nmsContainmentThreshold;
        // Anak lebih kecil dari induk dan minimal sekian bagian luas induk terdekat (Yolo:LambMinimumAreaRatio);
        // anak yang overlap dengan anak lain dan lebih kecil dari sekian bagian luasnya = potongan anak itu (Yolo:LambPartAreaRatio)
        private readonly float lambMinimumAreaRatio;
        private readonly float lambPartAreaRatio;

        // Buffer input dan tensor-nya dibuat sekali; setiap frame cukup mengisi ulang buffer
        private readonly float[] input;
        private readonly NamedOnnxValue[] inputs;

        // Buffer letterbox dipakai ulang di setiap frame
        private readonly Mat resized = new();
        private readonly Mat padded = new();

        public int InputSize { get; }
        public IReadOnlyDictionary<int, string> Labels { get; }

        public YoloDetectorService(InferenceOption option, AppSetting setting)
        {
            // Yolo:InferenceThreads membatasi thread CPU per kamera (0 = default ONNX Runtime) agar beberapa kamera
            // tidak berebut semua core
            var sessionOptions = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                IntraOpNumThreads = int.Parse(setting.YoloInferenceThreads, CultureInfo.InvariantCulture)
            };
            if (option.UseCoreMl)
            {
                sessionOptions.AppendExecutionProvider_CoreML();
            }

            session = new InferenceSession(option.Model, sessionOptions);

            var inputMeta = session.InputMetadata.First();
            inputShape = inputMeta.Value.Dimensions;
            InputSize = inputShape[2];

            int[] outputShape = session.OutputMetadata.First().Value.Dimensions;
            classCount = outputShape[1] - 4;
            candidateCount = outputShape[2];

            input = new float[inputShape.Aggregate(1, (a, b) => a * b)];
            inputs = new[] { NamedOnnxValue.CreateFromTensor(inputMeta.Key, new DenseTensor<float>(input, inputShape)) };
            confidenceThreshold = float.Parse(setting.YoloConfidenceThreshold, CultureInfo.InvariantCulture);
            nmsIouThreshold = float.Parse(setting.YoloNmsIouThreshold, CultureInfo.InvariantCulture);
            nmsContainmentThreshold = float.Parse(setting.YoloNmsContainmentThreshold, CultureInfo.InvariantCulture);
            lambMinimumAreaRatio = float.Parse(setting.YoloLambMinimumAreaRatio, CultureInfo.InvariantCulture);
            lambPartAreaRatio = float.Parse(setting.YoloLambPartAreaRatio, CultureInfo.InvariantCulture);

            Labels = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(option.Labels))!
                .ToDictionary(kv => int.Parse(kv.Key, CultureInfo.InvariantCulture), kv => kv.Value);
        }

        public List<Detection> Detect(Mat frame)
        {
            var (scale, padX, padY) = Preprocess(frame);

            using var results = session.Run(inputs);
            var output = ((DenseTensor<float>)results[0].AsTensor<float>()).Buffer.Span;

            int n = candidateCount;
            var candidates = new List<Detection>();
            for (int i = 0; i < n; i++)
            {
                int classId = 0;
                float best = output[4 * n + i];
                for (int c = 1; c < classCount; c++)
                {
                    float score = output[(4 + c) * n + i];
                    if (score > best)
                    {
                        best = score;
                        classId = c;
                    }
                }

                if (best < confidenceThreshold)
                {
                    continue;
                }

                // Balik dari koordinat letterbox ke koordinat frame asli
                float cx = output[i], cy = output[n + i], w = output[2 * n + i], h = output[3 * n + i];
                float x1 = Math.Clamp((cx - w / 2 - padX) / scale, 0, frame.Width - 1);
                float y1 = Math.Clamp((cy - h / 2 - padY) / scale, 0, frame.Height - 1);
                float x2 = Math.Clamp((cx + w / 2 - padX) / scale, 0, frame.Width - 1);
                float y2 = Math.Clamp((cy + h / 2 - padY) / scale, 0, frame.Height - 1);

                candidates.Add(new Detection
                {
                    ClassId = classId,
                    Label = Labels.GetValueOrDefault(classId, classId.ToString(CultureInfo.InvariantCulture)),
                    Confidence = best,
                    Box = new Rect((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1))
                });
            }

            return FilterLambs(Nms(candidates), frame.Size());
        }

        // Letterbox frame BGR ke S x S, lalu isi buffer input NCHW (RGB, 0-1) dalam satu putaran.
        private (float Scale, int PadX, int PadY) Preprocess(Mat frame)
        {
            int size = InputSize;
            float scale = Math.Min((float)size / frame.Width, (float)size / frame.Height);
            int width = (int)Math.Round(frame.Width * scale);
            int height = (int)Math.Round(frame.Height * scale);
            int padX = (size - width) / 2;
            int padY = (size - height) / 2;

            Cv2.Resize(frame, resized, new Size(width, height), 0, 0, InterpolationFlags.Linear);
            Cv2.CopyMakeBorder(resized, padded, padY, size - height - padY, padX, size - width - padX,
                BorderTypes.Constant, new Scalar(PadValue, PadValue, PadValue));

            int plane = size * size;
            const float norm = 1f / 255f;
            unsafe
            {
                byte* pixel = padded.DataPointer;
                fixed (float* target = input)
                {
                    float* r = target, g = target + plane, b = target + 2 * plane;
                    for (int p = 0; p < plane; p++, pixel += 3)
                    {
                        b[p] = pixel[0] * norm;
                        g[p] = pixel[1] * norm;
                        r[p] = pixel[2] * norm;
                    }
                }
            }

            return (scale, padX, padY);
        }

        // NMS per kelas (default Ultralytics, agnostic = false): kotak dengan IoU tinggi = deteksi ganda, confidence
        // tertinggi dipertahankan. Setelah itu kotak yang sebagian besar luasnya berada di dalam kotak sekelas yang lebih
        // besar dianggap potongan objek yang sama (mis. kepala anak), kotak utuh yang lebih besar yang dipertahankan
        // walau confidence-nya lebih rendah.
        private List<Detection> Nms(List<Detection> candidates)
        {
            var kept = new List<Detection>();
            foreach (var group in candidates.GroupBy(d => d.ClassId))
            {
                var byConfidence = Suppress(group.OrderByDescending(d => d.Confidence).ToList(),
                    (keep, other) => Iou(keep.Box, other.Box) > nmsIouThreshold);
                kept.AddRange(Suppress(byConfidence.OrderByDescending(d => Area(d.Box)).ToList(),
                    (keep, other) => Containment(keep.Box, other.Box) > nmsContainmentThreshold));
            }

            return kept;
        }

        // Aturan bbox anak terhadap induk dan anak lain (setelah NMS):
        // - Anak dan induk dengan IoU > Yolo:NmsIouThreshold = satu hewan terdeteksi dua kelas; confidence tertinggi
        //   dipertahankan. Anak yang terhalang induk (kotaknya di dalam/menempel kotak induk) IoU-nya kecil sehingga
        //   tetap dipertahankan dan tetap diproses aturan menyusu.
        // - Anak yang overlap dengan anak lain dan luasnya < Yolo:LambPartAreaRatio x luas anak itu = potongan
        //   (biasanya kepala) dari anak yang sama, dibuang.
        // - Anak harus lebih kecil dari induk terdekat dan luasnya minimal Yolo:LambMinimumAreaRatio x luas induk itu,
        //   sehingga kotak kecil yang hanya berisi kepala anak dibuang. Induk yang terpotong tepi frame tidak dipakai
        //   sebagai acuan karena luasnya tidak utuh; tanpa induk acuan aturan ukuran dilewati.
        private List<Detection> FilterLambs(List<Detection> detections, Size frameSize)
        {
            var lambs = detections.Where(d => d.Label == LambLabel).ToList();
            var ewes = detections.Where(d => d.Label == EweLabel).ToList();

            foreach (var lamb in lambs.ToList())
            {
                var duplicate = ewes.FirstOrDefault(e => Iou(lamb.Box, e.Box) > nmsIouThreshold);
                if (duplicate == null)
                {
                    continue;
                }

                if (lamb.Confidence >= duplicate.Confidence)
                {
                    ewes.Remove(duplicate);
                }
                else
                {
                    lambs.Remove(lamb);
                }
            }

            lambs = Suppress(lambs.OrderByDescending(d => Area(d.Box)).ToList(),
                (keep, other) => HasOverlap(keep.Box, other.Box) && Area(other.Box) < lambPartAreaRatio * Area(keep.Box));

            var fullEwes = ewes.Where(e => !IsCutByFrame(e.Box, frameSize)).ToList();
            lambs.RemoveAll(lamb =>
            {
                var ewe = fullEwes.MinBy(e => CentroidTrackerService.Distance(CentroidTrackerService.GetCentroid(e.Box),
                    CentroidTrackerService.GetCentroid(lamb.Box)));
                return ewe != null && (Area(lamb.Box) >= Area(ewe.Box) || Area(lamb.Box) < lambMinimumAreaRatio * Area(ewe.Box));
            });

            return ewes.Concat(lambs).ToList();
        }

        // Greedy: kotak di depan urutan dipertahankan dan menekan kotak berikutnya yang memenuhi isDuplicate
        private static List<Detection> Suppress(List<Detection> sorted, Func<Detection, Detection, bool> isDuplicate)
        {
            var kept = new List<Detection>();
            var suppressed = new bool[sorted.Count];
            for (int i = 0; i < sorted.Count; i++)
            {
                if (suppressed[i])
                {
                    continue;
                }

                kept.Add(sorted[i]);
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    suppressed[j] |= isDuplicate(sorted[i], sorted[j]);
                }
            }

            return kept;
        }

        private static bool IsCutByFrame(Rect box, Size frameSize)
        {
            return box.Left <= 0 || box.Top <= 0 || box.Right >= frameSize.Width - 1 || box.Bottom >= frameSize.Height - 1;
        }

        private static bool HasOverlap(Rect a, Rect b)
        {
            return a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
        }

        private static float Area(Rect box)
        {
            return (float)box.Width * box.Height;
        }

        private static float Iou(Rect a, Rect b)
        {
            Rect inter = a.Intersect(b);
            float interArea = Area(inter);
            float union = Area(a) + Area(b) - interArea;
            return union <= 0 ? 0 : interArea / union;
        }

        // Bagian luas kotak yang lebih kecil yang berada di dalam kotak lainnya
        private static float Containment(Rect a, Rect b)
        {
            Rect inter = a.Intersect(b);
            float smaller = Math.Min(Area(a), Area(b));
            return smaller <= 0 ? 0 : Area(inter) / smaller;
        }

        public void Dispose()
        {
            resized.Dispose();
            padded.Dispose();
            session.Dispose();
        }
    }
}
