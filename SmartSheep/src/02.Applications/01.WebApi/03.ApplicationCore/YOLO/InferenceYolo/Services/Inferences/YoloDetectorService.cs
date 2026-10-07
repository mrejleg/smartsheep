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

        private readonly InferenceSession session;
        private readonly string inputName;
        private readonly int[] inputShape;
        private readonly int classCount;
        private readonly int candidateCount;
        private readonly float confidenceThreshold;

        // IoU NMS (Yolo:NmsIouThreshold; default Ultralytics predict 0.7)
        private readonly float nmsIouThreshold;

        // Kotak yang sebagian besar luasnya (Yolo:NmsContainmentThreshold) berada di dalam kotak sekelas dengan
        // confidence lebih tinggi dianggap deteksi ganda objek yang sama (IoU-nya kecil karena ukurannya beda jauh)
        private readonly float nmsContainmentThreshold;
        private readonly float[] input;
        private readonly DenseTensor<float> tensor;

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
            inputName = inputMeta.Key;
            inputShape = inputMeta.Value.Dimensions;
            InputSize = inputShape[2];

            int[] outputShape = session.OutputMetadata.First().Value.Dimensions;
            classCount = outputShape[1] - 4;
            candidateCount = outputShape[2];

            input = new float[inputShape.Aggregate(1, (a, b) => a * b)];
            tensor = new DenseTensor<float>(input, inputShape);
            confidenceThreshold = float.Parse(setting.YoloConfidenceThreshold, CultureInfo.InvariantCulture);
            nmsIouThreshold = float.Parse(setting.YoloNmsIouThreshold, CultureInfo.InvariantCulture);
            nmsContainmentThreshold = float.Parse(setting.YoloNmsContainmentThreshold, CultureInfo.InvariantCulture);

            Labels = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(option.Labels))!
                .ToDictionary(kv => int.Parse(kv.Key, CultureInfo.InvariantCulture), kv => kv.Value);
        }

        public List<Detection> Detect(Mat frame)
        {
            var (scale, padX, padY) = Preprocess(frame);

            using var results = session.Run(new[] { NamedOnnxValue.CreateFromTensor(inputName, tensor) });
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

            return Nms(candidates);
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

        // NMS per kelas (default Ultralytics, agnostic = false) ditambah penekanan kotak yang berada di dalam kotak lain
        private List<Detection> Nms(List<Detection> candidates)
        {
            var kept = new List<Detection>();
            foreach (var group in candidates.GroupBy(d => d.ClassId))
            {
                var sorted = group.OrderByDescending(d => d.Confidence).ToList();
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
                        if (!suppressed[j] && (Iou(sorted[i].Box, sorted[j].Box) > nmsIouThreshold ||
                            Containment(sorted[i].Box, sorted[j].Box) > nmsContainmentThreshold))
                        {
                            suppressed[j] = true;
                        }
                    }
                }
            }

            return kept;
        }

        private static float Iou(Rect a, Rect b)
        {
            Rect inter = a.Intersect(b);
            float interArea = (float)inter.Width * inter.Height;
            float union = (float)a.Width * a.Height + (float)b.Width * b.Height - interArea;
            return union <= 0 ? 0 : interArea / union;
        }

        // Bagian luas kotak yang lebih kecil yang berada di dalam kotak lainnya
        private static float Containment(Rect a, Rect b)
        {
            Rect inter = a.Intersect(b);
            float smaller = Math.Min((float)a.Width * a.Height, (float)b.Width * b.Height);
            return smaller <= 0 ? 0 : (float)inter.Width * inter.Height / smaller;
        }

        public void Dispose()
        {
            resized.Dispose();
            padded.Dispose();
            session.Dispose();
        }
    }
}
