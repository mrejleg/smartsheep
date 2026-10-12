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

        // Luas anak terhadap luas induk acuan: di bawah Yolo:LambMinimumAreaRatio = potongan (kepala), mulai
        // Yolo:LambMaximumAreaRatio = induk. Anak yang overlap/menempel (jarak <= Yolo:LambPartGap x lebar frame)
        // dengan anak lain dan lebih kecil dari Yolo:LambPartAreaRatio x luasnya = potongan anak itu.
        private readonly float lambMinimumAreaRatio;
        private readonly float lambMaximumAreaRatio;
        private readonly float lambPartAreaRatio;
        private readonly float lambPartGap;

        // Luas induk utuh rata-rata bergerak (bobot Yolo:BaselineWeight), acuan ukuran saat induk tidak terdeteksi
        // utuh di frame; detector scoped per kamera sehingga acuan ini milik satu kamera
        private readonly float baselineWeight;
        private readonly int eweClassId;
        private readonly int lambClassId;
        private float? eweArea;

        // Luas induk terpotong tepi frame terbesar yang pernah terlihat: batas bawah luas induk utuh, acuan terakhir
        // untuk kamera yang induknya hampir selalu terpotong frame (tanpa acuan, aturan ukuran tidak berjalan)
        private float? cutEweArea;

        // Jumlah induk dan anak di kandang (Barn.EweCount/LambCount dari DB, diisi sekali saat sumber video dimulai) = batas
        // bbox per frame; 0 = tidak dibatasi
        private readonly int eweCount;
        private readonly int lambCount;

        // Kotak induk dan anak terpilih di frame sebelumnya: bila deteksi lebih banyak dari jumlah kandang, yang
        // dipertahankan yang terdekat dengan kotak ini. Induk yang sesaat tidak terdeteksi memakai kotak terakhirnya
        // paling lama Yolo:EweHoldFrames frame (MissingFrames = jumlah frame ditahan).
        private readonly int eweHoldFrames;
        private List<(Detection Ewe, int MissingFrames)> lastEwes = new();
        private List<Detection> lastLambs = new();

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
            lambMaximumAreaRatio = float.Parse(setting.YoloLambMaximumAreaRatio, CultureInfo.InvariantCulture);
            lambPartAreaRatio = float.Parse(setting.YoloLambPartAreaRatio, CultureInfo.InvariantCulture);
            lambPartGap = float.Parse(setting.YoloLambPartGap, CultureInfo.InvariantCulture);
            eweCount = option.EweCount;
            lambCount = option.LambCount;
            eweHoldFrames = int.Parse(setting.YoloEweHoldFrames, CultureInfo.InvariantCulture);
            baselineWeight = float.Parse(setting.YoloBaselineWeight, CultureInfo.InvariantCulture);

            Labels = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(option.Labels))!
                .ToDictionary(kv => int.Parse(kv.Key, CultureInfo.InvariantCulture), kv => kv.Value);
            eweClassId = Labels.First(kv => kv.Value == EweLabel).Key;
            lambClassId = Labels.First(kv => kv.Value == LambLabel).Key;
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

        // Aturan bbox anak terhadap induk dan anak lain (setelah NMS). Induk tidak pernah dibuang di sini.
        // - Anak dan induk dengan IoU > Yolo:NmsIouThreshold = satu hewan terdeteksi dua kelas; selalu jadi induk.
        //   Anak yang terhalang induk (kotaknya di dalam/menempel kotak induk) IoU-nya kecil sehingga tetap
        //   dipertahankan dan tetap diproses aturan menyusu.
        // - Kotak induk gabungan: induk yang overlap dengan induk lain dan membungkus (>= Yolo:NmsContainmentThreshold
        //   luasnya) anak yang tidak overlap dengan induk lain itu = satu kotak yang menggabungkan induk dan anak di
        //   sebelahnya, dibuang. Induk asli dan anaknya tetap.
        // - Induk lebih banyak dari jumlah induk kandang: yang dipertahankan induk terdekat dengan induk frame sebelumnya,
        //   sisanya diisi confidence tertinggi (ukuran tidak dipakai: induk bisa kecil karena terpotong, anak bisa besar).
        //   Induk lain yang sebagian besar (>= Yolo:NmsContainmentThreshold) berada di dalam induk terpilih = potongan
        //   induk itu, dibuang; selebihnya = anak bila asalnya anak, terdekat dengan anak frame sebelumnya, atau ukurannya
        //   ukuran anak (< Yolo:LambMaximumAreaRatio x induk terpilih terbesar), selain itu dibuang.
        // - Kotak anak yang luasnya >= Yolo:LambMaximumAreaRatio x luas induk acuan = induk yang salah kelas,
        //   dijadikan induk. Acuan = induk utuh terdekat (tidak terpotong tepi frame), atau rata-rata luas induk
        //   utuh frame sebelumnya bila di frame ini tidak ada, atau luas induk terpotong terbesar yang pernah
        //   terlihat; tanpa acuan aturan ukuran dilewati.
        // - Anak yang overlap/menempel dengan anak lain dan luasnya < Yolo:LambPartAreaRatio x luas anak itu =
        //   potongan (biasanya kepala) dari anak yang sama, digabung ke bbox anak itu agar kepalanya ikut terbungkus.
        // - Anak yang luasnya < Yolo:LambMinimumAreaRatio x luas induk acuan = kotak yang hanya berisi kepala, dibuang.
        // - Induk yang sesaat tidak terdeteksi memakai kotak terakhirnya (paling lama Yolo:EweHoldFrames frame), per induk
        //   selama jumlah induk kandang belum penuh.
        // - Anak lebih banyak dari jumlah anak kandang: yang terdekat dengan anak frame sebelumnya dipertahankan, lalu
        //   confidence tertinggi; anak baru yang hampir seluruhnya di dalam kotak induk (biasanya kepala induk) dibuang dulu.
        //   Anak yang overlap induk tetapi sudah ada sebelumnya (sedang menyusu) tidak dibuang.
        private List<Detection> FilterLambs(List<Detection> detections, Size frameSize)
        {
            var ewes = detections.Where(d => d.Label == EweLabel).ToList();
            var lambs = detections.Where(d => d.Label == LambLabel && !ewes.Any(e => Iou(d.Box, e.Box) > nmsIouThreshold)).ToList();

            var fullEwes = ewes.Where(e => !IsCutByFrame(e.Box, frameSize)).ToList();
            float? ReferenceArea(Detection lamb) => fullEwes.Count > 0
                ? Area(fullEwes.MinBy(e => CentroidTrackerService.Distance(CentroidTrackerService.GetCentroid(e.Box),
                    CentroidTrackerService.GetCentroid(lamb.Box)))!.Box)
                : eweArea ?? cutEweArea;

            var fromLambs = lambs.Where(l => Area(l.Box) >= lambMaximumAreaRatio * ReferenceArea(l)).ToList();
            foreach (var lamb in fromLambs)
            {
                lambs.Remove(lamb);
                lamb.ClassId = eweClassId;
                lamb.Label = EweLabel;
                ewes.Add(lamb);
            }

            // Induk hasil koreksi kelas bisa menumpuk dengan induk yang sudah ada
            ewes = Nms(ewes);
            ewes = ewes.Where(merged => !ewes.Any(ewe => ewe != merged && HasOverlap(ewe.Box, merged.Box) &&
                lambs.Any(l => Containment(merged.Box, l.Box) >= nmsContainmentThreshold && !HasOverlap(l.Box, ewe.Box)))).ToList();

            if (eweCount > 0 && ewes.Count > eweCount)
            {
                var selected = Nearest(lastEwes.Select(x => x.Ewe).ToList(), ewes).Select(x => x.Current).Take(eweCount).ToList();
                selected.AddRange(ewes.Except(selected).OrderByDescending(e => e.Confidence).Take(eweCount - selected.Count));
                float largest = selected.Max(e => Area(e.Box));
                var extras = ewes.Except(selected).ToList();
                var nearLambs = Nearest(lastLambs, extras).Select(x => x.Current).ToList();
                foreach (var ewe in extras)
                {
                    if (!selected.Any(s => Containment(s.Box, ewe.Box) >= nmsContainmentThreshold) &&
                        (fromLambs.Contains(ewe) || nearLambs.Contains(ewe) || Area(ewe.Box) < lambMaximumAreaRatio * largest))
                    {
                        ewe.ClassId = lambClassId;
                        ewe.Label = LambLabel;
                        lambs.Add(ewe);
                    }
                }
                ewes = selected;
            }

            // Acuan luas anak berikutnya = induk terpilih saja (termasuk hasil koreksi kelas), bukan induk sebelum seleksi
            fullEwes = ewes.Where(e => !IsCutByFrame(e.Box, frameSize)).ToList();

            int gap = (int)(lambPartGap * frameSize.Width);
            lambs = Suppress(lambs.OrderByDescending(d => Area(d.Box)).ToList(), (keep, other) =>
            {
                bool isPart = HasOverlap(new Rect(keep.Box.X - gap, keep.Box.Y - gap, keep.Box.Width + 2 * gap, keep.Box.Height + 2 * gap), other.Box) &&
                    Area(other.Box) < lambPartAreaRatio * Area(keep.Box);
                if (isPart)
                {
                    keep.Box |= other.Box;
                }
                return isPart;
            });
            lambs.RemoveAll(l => Area(l.Box) < lambMinimumAreaRatio * ReferenceArea(l));

            // Acuan luas induk diperbarui dari induk yang terdeteksi di frame ini (sebelum hold, agar induk yang ditahan
            // tidak dihitung ulang)
            var cutEwes = ewes.Where(e => IsCutByFrame(e.Box, frameSize)).ToList();
            if (cutEwes.Count > 0)
            {
                cutEweArea = Math.Max(cutEweArea ?? 0, cutEwes.Max(e => Area(e.Box)));
            }

            if (fullEwes.Count > 0)
            {
                float area = fullEwes.Average(e => Area(e.Box));
                eweArea = eweArea.HasValue ? eweArea.Value + baselineWeight * (area - eweArea.Value) : area;
            }

            // Induk sebelumnya yang tidak punya pasangan di frame ini ditahan, selama jumlah induk belum penuh
            // (tanpa jumlah kandang: hanya bila tidak ada induk sama sekali)
            var matchedEwes = Nearest(lastEwes.Select(x => x.Ewe).ToList(), ewes).Select(x => x.Previous).ToList();
            var held = new List<(Detection Ewe, int MissingFrames)>();
            foreach (var (ewe, missingFrames) in lastEwes.Where(x => !matchedEwes.Contains(x.Ewe)))
            {
                if (missingFrames < eweHoldFrames && (eweCount > 0 ? ewes.Count + held.Count < eweCount : ewes.Count == 0))
                {
                    held.Add((ewe, missingFrames + 1));
                }
            }
            lastEwes = ewes.Select(e => (e, 0)).Concat(held).ToList();
            ewes.AddRange(held.Select(x => x.Ewe));

            // Anak lebih banyak dari jumlah kandang: yang terdekat dengan anak frame sebelumnya dipertahankan (termasuk
            // yang sedang menyusu di dekat induk), sisanya diisi confidence tertinggi; anak baru yang hampir seluruhnya
            // di dalam kotak induk (biasanya kepala induk) paling akhir
            if (lambCount > 0 && lambs.Count > lambCount)
            {
                var kept = Nearest(lastLambs, lambs).Select(x => x.Current).Take(lambCount).ToList();
                kept.AddRange(lambs.Except(kept)
                    .OrderBy(l => ewes.Any(e => Containment(e.Box, l.Box) >= nmsContainmentThreshold))
                    .ThenByDescending(l => l.Confidence)
                    .Take(lambCount - kept.Count));
                lambs = kept;
            }
            lastLambs = lambs;

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

        // Pasangan kotak frame sebelumnya dengan deteksi frame ini, jarak centroid terkecil lebih dulu dan satu kotak
        // hanya untuk satu deteksi (sama dengan centroid tracking); jarak lebih dari sisi terpanjang kedua kotak (kotak bisa
        // membesar saat badan yang tadinya terpotong terlihat utuh) dianggap objek lain
        private static List<(Detection Previous, Detection Current)> Nearest(List<Detection> previous, List<Detection> current)
        {
            var pairs = previous.SelectMany(p => current.Select(c => (Previous: p, Current: c,
                    Distance: CentroidTrackerService.Distance(CentroidTrackerService.GetCentroid(p.Box), CentroidTrackerService.GetCentroid(c.Box)))))
                .Where(x => x.Distance <= Math.Max(Math.Max(x.Previous.Box.Width, x.Previous.Box.Height), Math.Max(x.Current.Box.Width, x.Current.Box.Height)))
                .OrderBy(x => x.Distance)
                .ToList();

            var matched = new List<(Detection Previous, Detection Current)>();
            foreach (var pair in pairs)
            {
                if (!matched.Any(m => m.Previous == pair.Previous || m.Current == pair.Current))
                {
                    matched.Add((pair.Previous, pair.Current));
                }
            }

            return matched;
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
