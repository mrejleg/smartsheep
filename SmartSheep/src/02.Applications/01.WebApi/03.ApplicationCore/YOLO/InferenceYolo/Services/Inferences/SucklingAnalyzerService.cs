using System.Globalization;
using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using OpenCvSharp;

namespace InferenceYolo.Services.Inferences
{
    // Identifikasi indikasi aktivitas menyusu dari trajectory (proposal 3.3.1, Gambar 14).
    //
    // Zona ambing : sumbu badan induk mendatar bila induk terakhir bergerak mendatar, tegak bila bergerak tegak.
    //               Sebelum induk pernah bergerak, sumbu = sisi bbox yang terpotong tepi frame (badan berlanjut ke
    //               luar frame), selain itu sisi panjang bbox. Induk yang berputar di tempat (sisi panjang bbox
    //               jelas tegak lurus sumbu lama) dianggap belum pernah bergerak lagi.
    //               Kepala : induk bisa berjalan maju maupun mundur, tetapi mundur hanya langkah pendek. Arah gerak
    //               dirata-rata dengan memori Yolo:EweHeadMemorySeconds; bila rata-ratanya cukup kuat, itu arah kepala
    //               dan disimpan sampai rata-rata arah sebaliknya juga cukup kuat (langkah mundur pendek tidak
    //               membalik kepala). Zona ambing = Yolo:UdderZoneLength x panjang badan dari ujung belakang.
    //               Bila arah kepala belum diketahui atau tidak searah sumbu, seluruh panjang badan (kedua ujung)
    //               dianggap kandidat zona ambing.
    //               Zona diperlebar ke kedua samping badan (tegak lurus sumbu) sebesar Yolo:UdderZoneMargin x lebar
    //               badan karena anak menyusu berdiri di samping induk, tidak diperpanjang ke depan/belakang badan,
    //               dan dibatasi frame. Induk yang tidak terlihat lebih dari Yolo:BaselineResetSeconds kehilangan
    //               sumbu dan arah kepalanya karena bisa sudah berbalik.
    // Pasangan    : anak dipasangkan dengan induk yang bbox-nya overlap dan zona ambingnya memuat centroid anak
    //               (terdekat bila lebih dari satu); bila tidak ada, induk terdekat.
    // Baseline    : rasio (sisi panjang / sisi pendek) dan luas bbox anak saat anak berjalan dan
    //               belum overlap dengan induk (berjalan = berdiri tegak). Bila track anak tidak terlihat lebih
    //               dari Yolo:BaselineResetSeconds, baseline dihapus karena track yang muncul lagi bisa saja objek
    //               lain (mis. bagian tubuh induk); anak harus berjalan bebas lagi sebelum dinilai menyusu.
    // Stay point  : bersamaan -> bbox anak overlap dengan induk, centroid anak di zona ambing,
    //               postur berdiri (rasio mirip baseline; lebih membulat/lebar = tiduran), dan
    //               luas bbox anak menyusut dibanding baseline (kepala tertutup tubuh induk).
    // Menyusu     : stay point berlangsung > Yolo:MinimumSucklingSeconds; bila lebih pendek
    //               dicatat sebagai pendekatan gagal. Kejadian berakhir di frame pertama aturan
    //               tidak terpenuhi atau saat anak/induk hilang (tidak terdeteksi lebih dari
    //               Yolo:MissingToleranceSeconds; kedipan deteksi yang lebih singkat tidak memutus
    //               kejadian); bila muncul lagi dan aturan terpenuhi, dihitung sebagai kejadian baru.
    //               Pergantian track ID diabaikan.
    // Pendekatan  : dihitung setiap kali bbox anak mulai overlap dengan bbox induk.
    public class SucklingAnalyzerService : ISucklingAnalyzerService
    {
        private const string EweLabel = "induk";
        private const string LambLabel = "anak";

        // Arah kepala dianggap yakin bila panjang rata-rata arah gerak minimal sebesar ini (1 = selalu searah), dan
        // dipakai untuk sumbu bila komponennya searah sumbu juga minimal sebesar ini (sudut <= 60 derajat)
        private const float HeadConfidence = 0.5f;

        // Sisi panjang bbox minimal sekian kali sisi pendek agar perubahan sumbu karena berputar di tempat terbaca jelas
        private const float TurnAspectRatio = 1.5f;

        private readonly string sourceVideoCode;
        private readonly double minimumSucklingSeconds;
        private readonly float postureTolerance;
        private readonly float shrinkThreshold;
        private readonly double missingToleranceSeconds;
        private readonly float udderZoneMargin;

        // Arah gerak induk: perpindahan centroid dalam jendela ini minimal sekian kali sisi pendek bbox induk
        private readonly double eweDirectionWindowSeconds;
        private readonly float eweMinimumMove;

        // Memori rata-rata arah gerak induk untuk arah kepala, dan panjang zona ambing dari ujung belakang badan
        private readonly double eweHeadMemorySeconds;
        private readonly float udderZoneLength;

        // Anak berjalan: kecepatan centroid minimal sekian kali sisi pendek bbox anak per detik
        private readonly double lambMovingWindowSeconds;
        private readonly float lambMinimumSpeed;

        // Bobot rata-rata bergerak baseline (makin besar makin cepat mengikuti sampel terbaru)
        private readonly float baselineWeight;
        private readonly double baselineResetSeconds;

        // Histeresis: syarat yang sudah terpenuhi baru dianggap gagal bila melewati batas yang lebih
        // longgar, agar bbox yang bergetar di sekitar batas tidak memutus kejadian. Ambang masuk tetap.
        // Zona ambing diperbesar sekian kali lebar badan induk; ambang postur/menyusut dilonggarkan sekian bagian.
        private readonly float udderZoneHysteresis;
        private readonly float thresholdHysteresis;

        private readonly Dictionary<int, EweState> ewes = new();
        private readonly Dictionary<int, LambState> lambs = new();

        public SucklingAnalyzerService(InferenceOption option, AppSetting setting)
        {
            sourceVideoCode = option.SourceVideoCode;
            minimumSucklingSeconds = double.Parse(setting.YoloMinimumSucklingSeconds, CultureInfo.InvariantCulture);
            postureTolerance = float.Parse(setting.YoloPostureTolerance, CultureInfo.InvariantCulture);
            shrinkThreshold = float.Parse(setting.YoloShrinkThreshold, CultureInfo.InvariantCulture);
            missingToleranceSeconds = double.Parse(setting.YoloMissingToleranceSeconds, CultureInfo.InvariantCulture);
            udderZoneMargin = float.Parse(setting.YoloUdderZoneMargin, CultureInfo.InvariantCulture);
            eweDirectionWindowSeconds = double.Parse(setting.YoloEweDirectionWindowSeconds, CultureInfo.InvariantCulture);
            eweMinimumMove = float.Parse(setting.YoloEweMinimumMove, CultureInfo.InvariantCulture);
            eweHeadMemorySeconds = double.Parse(setting.YoloEweHeadMemorySeconds, CultureInfo.InvariantCulture);
            udderZoneLength = float.Parse(setting.YoloUdderZoneLength, CultureInfo.InvariantCulture);
            lambMovingWindowSeconds = double.Parse(setting.YoloLambMovingWindowSeconds, CultureInfo.InvariantCulture);
            lambMinimumSpeed = float.Parse(setting.YoloLambMinimumSpeed, CultureInfo.InvariantCulture);
            baselineWeight = float.Parse(setting.YoloBaselineWeight, CultureInfo.InvariantCulture);
            baselineResetSeconds = double.Parse(setting.YoloBaselineResetSeconds, CultureInfo.InvariantCulture);
            udderZoneHysteresis = float.Parse(setting.YoloUdderZoneHysteresis, CultureInfo.InvariantCulture);
            thresholdHysteresis = float.Parse(setting.YoloThresholdHysteresis, CultureInfo.InvariantCulture);
        }

        public FrameAnalysis Analyze(List<TrackedObject> objects, double timeSeconds, DateTime frameAt, Size frameSize)
        {
            var analysis = new FrameAnalysis { TimeSeconds = timeSeconds, FrameAt = frameAt, Objects = objects };

            // Hanya induk yang terdeteksi di frame ini; posisi terakhir induk yang hilang tidak dipakai
            // agar anak tidak dipasangkan dengan kotak induk yang sudah tidak ada di sana
            var eweTracks = objects.Where(o => o.IsConfirmed && o.IsVisible && o.Label == EweLabel).ToList();
            foreach (var ewe in eweTracks)
            {
                var state = GetState(ewes, ewe.TrackId);
                if (timeSeconds - state.LastSeenSeconds > baselineResetSeconds)
                {
                    ResetEwe(state);
                }
                // Induk baru/di-reset belum punya selang waktu; selang dibatasi jendela arah agar satu gerakan
                // setelah induk sempat hilang tidak langsung menentukan arah kepala
                double elapsed = state.History.Count > 0 ? Math.Min(timeSeconds - state.LastSeenSeconds, eweDirectionWindowSeconds) : 0;
                state.LastSeenSeconds = timeSeconds;

                UpdateHistory(state.History, timeSeconds, ewe.Centroid, eweDirectionWindowSeconds);
                UpdateDirection(state, ewe.Box, elapsed, frameSize);

                analysis.UdderZones[ewe.TrackId] = GetUdderZone(ewe.Box, state, frameSize);
            }

            foreach (var lamb in objects.Where(o => o.IsConfirmed && o.Label == LambLabel))
            {
                var state = GetState(lambs, lamb.TrackId);
                var status = new LambStatus { LambTrackId = lamb.TrackId, IsVisible = lamb.IsVisible };
                if (lamb.IsVisible)
                {
                    if (timeSeconds - state.LastVisibleSeconds > baselineResetSeconds)
                    {
                        ResetBaseline(state);
                    }
                    state.LastVisibleSeconds = timeSeconds;
                }

                if (lamb.IsVisible && eweTracks.Count > 0)
                {
                    state.LastSeenSeconds = timeSeconds;
                    Evaluate(lamb, state, status, eweTracks, analysis.UdderZones, timeSeconds);
                    UpdateApproach(lamb, state, status, timeSeconds, frameAt, analysis);
                    UpdateStayPoint(lamb, state, status, timeSeconds, frameAt, analysis);
                }
                else if (timeSeconds - state.LastSeenSeconds > missingToleranceSeconds)
                {
                    Stop(state, analysis.ClosedEvents);
                }
                else if (state.Stay != null && state.Stay.DurationSeconds > minimumSucklingSeconds)
                {
                    // Deteksi berkedip sesaat: kejadian menyusu yang berjalan dipertahankan
                    status.IsSuckling = true;
                    status.StayPointSeconds = state.Stay.DurationSeconds;
                    analysis.ActiveEvents.Add(state.Stay);
                }

                analysis.Lambs.Add(status);
            }

            return analysis;
        }

        // Tutup semua kejadian yang masih berjalan (akhir video / streaming dihentikan)
        public List<SucklingEvent> Complete()
        {
            var closed = new List<SucklingEvent>();
            foreach (var state in lambs.Values)
            {
                Stop(state, closed);
            }

            return closed;
        }

        // Anak atau induk hilang: indikasi berhenti di frame terakhir yang memenuhi aturan.
        // Bila muncul lagi dan aturan terpenuhi, dihitung sebagai kejadian baru.
        private void Stop(LambState state, List<SucklingEvent> closed)
        {
            if (state.Approach != null)
            {
                closed.Add(Close(state.Approach, state.ApproachStartSeconds, state.ApproachLastSeconds));
                state.Approach = null;
            }

            if (state.Stay != null)
            {
                closed.Add(CloseStay(state));
            }
        }

        private void Evaluate(TrackedObject lamb, LambState state, LambStatus status, List<TrackedObject> eweTracks,
            Dictionary<int, Rect> udderZones, double timeSeconds)
        {
            var box = lamb.Box;
            float shortSide = Math.Max(1, Math.Min(box.Width, box.Height));
            status.Ratio = Math.Max(box.Width, box.Height) / shortSide;
            status.Area = (float)box.Width * box.Height;

            UpdateHistory(state.History, timeSeconds, lamb.Centroid, lambMovingWindowSeconds);
            status.IsMoving = IsMoving(state.History, shortSide);

            // Pasangkan dengan induk yang zona ambingnya memuat anak, selain itu induk terdekat
            bool IsInUdderZone(TrackedObject ewe) => HasOverlap(box, ewe.Box) &&
                Contains(GetHysteresisZone(udderZones[ewe.TrackId], ewe.Box, ewes[ewe.TrackId].IsHorizontal, state.WasInUdderZone), lamb.Centroid);
            float DistanceTo(TrackedObject ewe) => CentroidTrackerService.Distance(ewe.Centroid, lamb.Centroid);
            var paired = eweTracks.Where(IsInUdderZone).MinBy(DistanceTo) ?? eweTracks.MinBy(DistanceTo)!;
            status.EweTrackId = paired.TrackId;
            status.IsOverlap = HasOverlap(box, paired.Box);
            status.IsInUdderZone = IsInUdderZone(paired);

            if (status.IsMoving && !status.IsOverlap)
            {
                state.BaselineRatio = Blend(state.BaselineRatio, status.Ratio);
                state.BaselineArea = Blend(state.BaselineArea, status.Area);
            }

            status.BaselineRatio = state.BaselineRatio;
            status.BaselineArea = state.BaselineArea;
            float posture = postureTolerance * (state.WasStanding ? 1 + thresholdHysteresis : 1);
            float shrink = shrinkThreshold * (state.WasShrunk ? 1 - thresholdHysteresis : 1);
            status.IsStanding = state.BaselineRatio.HasValue && status.Ratio >= state.BaselineRatio.Value * (1 - posture);
            status.IsShrunk = state.BaselineArea.HasValue && status.Area <= state.BaselineArea.Value * (1 - shrink);

            state.WasInUdderZone = status.IsInUdderZone;
            state.WasStanding = status.IsStanding;
            state.WasShrunk = status.IsShrunk;
            status.IsStayPoint = status.IsOverlap && status.IsInUdderZone && status.IsStanding && status.IsShrunk;
        }

        private void UpdateApproach(TrackedObject lamb, LambState state, LambStatus status, double timeSeconds, DateTime frameAt,
            FrameAnalysis analysis)
        {
            if (status.IsOverlap)
            {
                if (state.Approach == null)
                {
                    state.Approach = NewEvent(SucklingEvent.Approach, lamb.TrackId, status.EweTrackId!.Value, frameAt);
                    state.ApproachStartSeconds = timeSeconds;
                }

                state.ApproachLastSeconds = timeSeconds;
                state.Approach.EndAt = frameAt;
                state.Approach.FrameCount++;
            }
            else if (state.Approach != null)
            {
                analysis.ClosedEvents.Add(Close(state.Approach, state.ApproachStartSeconds, state.ApproachLastSeconds));
                state.Approach = null;
            }
        }

        private void UpdateStayPoint(TrackedObject lamb, LambState state, LambStatus status, double timeSeconds, DateTime frameAt,
            FrameAnalysis analysis)
        {
            if (status.IsStayPoint)
            {
                if (state.Stay == null)
                {
                    state.Stay = NewEvent(SucklingEvent.Suckling, lamb.TrackId, status.EweTrackId!.Value, frameAt);
                    state.StayStartSeconds = timeSeconds;
                }

                state.StayLastSeconds = timeSeconds;
                state.Stay.EndAt = frameAt;
                state.Stay.FrameCount++;
                state.Stay.DurationSeconds = Math.Round(timeSeconds - state.StayStartSeconds, 3);

                status.StayPointSeconds = state.Stay.DurationSeconds;
                if (state.Stay.DurationSeconds > minimumSucklingSeconds)
                {
                    status.IsSuckling = true;
                    analysis.ActiveEvents.Add(state.Stay);
                }
            }
            else if (state.Stay != null)
            {
                analysis.ClosedEvents.Add(CloseStay(state));
            }
        }

        // Durasi = t_akhir - t_awal (Persamaan 10); <= durasi minimal = pendekatan gagal
        private SucklingEvent CloseStay(LambState state)
        {
            var closed = Close(state.Stay, state.StayStartSeconds, state.StayLastSeconds);
            closed.EventType = closed.DurationSeconds > minimumSucklingSeconds ? SucklingEvent.Suckling : SucklingEvent.FailedAttempt;
            state.Stay = null;
            return closed;
        }

        private static SucklingEvent Close(SucklingEvent sucklingEvent, double startSeconds, double lastSeconds)
        {
            sucklingEvent.DurationSeconds = Math.Round(lastSeconds - startSeconds, 3);
            sucklingEvent.IsClosed = true;
            return sucklingEvent;
        }

        private SucklingEvent NewEvent(string eventType, int lambTrackId, int eweTrackId, DateTime frameAt)
        {
            return new SucklingEvent
            {
                EventType = eventType,
                SourceVideoCode = sourceVideoCode,
                LambTrackId = lambTrackId,
                EweTrackId = eweTrackId,
                StartAt = frameAt,
                EndAt = frameAt
            };
        }

        // Bagian belakang badan sepanjang sumbu (seluruh badan bila arah kepala belum yakin) diperlebar ke samping
        // badan dan dibatasi frame
        private Rect GetUdderZone(Rect box, EweState state, Size frameSize)
        {
            state.IsHorizontal = state.Direction is Point2f d ? Math.Abs(d.X) >= Math.Abs(d.Y) : IsHorizontalBody(box, frameSize);
            int rear = GetRearSide(state);

            Rect udder;
            if (state.IsHorizontal)
            {
                int length = rear == 0 ? box.Width : (int)(udderZoneLength * box.Width);
                int margin = (int)(udderZoneMargin * box.Height);
                udder = new Rect(rear > 0 ? box.Right - length : box.X, box.Y - margin, length, box.Height + 2 * margin);
            }
            else
            {
                int length = rear == 0 ? box.Height : (int)(udderZoneLength * box.Height);
                int margin = (int)(udderZoneMargin * box.Width);
                udder = new Rect(box.X - margin, rear > 0 ? box.Bottom - length : box.Y, box.Width + 2 * margin, length);
            }

            return udder.Intersect(new Rect(0, 0, frameSize.Width, frameSize.Height));
        }

        // Ujung belakang badan pada sumbu: -1 = sisi koordinat kecil (kiri/atas), 1 = sisi koordinat besar,
        // 0 = arah kepala belum diketahui atau tidak searah sumbu
        private static int GetRearSide(EweState state)
        {
            if (state.HeadDirection is not Point2f head)
            {
                return 0;
            }

            float along = state.IsHorizontal ? head.X : head.Y;
            return Math.Abs(along) < HeadConfidence ? 0 : along > 0 ? -1 : 1;
        }

        // Histeresis hanya memperlebar zona ke samping badan, tidak memanjangkan ke depan/belakang
        private Rect GetHysteresisZone(Rect udder, Rect eweBox, bool isHorizontal, bool wasInUdderZone)
        {
            if (wasInUdderZone)
            {
                int grow = (int)(udderZoneHysteresis * Math.Min(eweBox.Width, eweBox.Height));
                udder.Inflate(isHorizontal ? 0 : grow, isHorizontal ? grow : 0);
            }

            return udder;
        }

        // Sumbu badan sebelum induk bergerak: badan yang terpotong tepi frame berlanjut ke arah tepi itu;
        // selain itu mengikuti sisi panjang bbox
        private static bool IsHorizontalBody(Rect box, Size frameSize)
        {
            bool cutX = box.Left <= 0 || box.Right >= frameSize.Width;
            bool cutY = box.Top <= 0 || box.Bottom >= frameSize.Height;
            return cutX == cutY ? box.Width >= box.Height : cutX;
        }

        private static void ResetBaseline(LambState state)
        {
            state.History.Clear();
            state.BaselineRatio = null;
            state.BaselineArea = null;
            state.WasStanding = false;
            state.WasShrunk = false;
        }

        // Sumbu = arah gerak terakhir. Arah kepala = rata-rata arah gerak dengan bobot waktu (memori
        // Yolo:EweHeadMemorySeconds), sehingga langkah mundur yang pendek tidak membalik kepala.
        private void UpdateDirection(EweState state, Rect box, double elapsed, Size frameSize)
        {
            var (_, oldest) = state.History.Peek();
            var newest = state.History.Last().Centroid;
            float dx = newest.X - oldest.X, dy = newest.Y - oldest.Y;
            float move = MathF.Sqrt(dx * dx + dy * dy);

            if (move >= eweMinimumMove * Math.Min(box.Width, box.Height))
            {
                var direction = new Point2f(dx / move, dy / move);
                var head = state.Head ?? new Point2f(0, 0);
                float weight = (float)Math.Min(1, elapsed / eweHeadMemorySeconds);
                state.Direction = direction;
                head = new Point2f(head.X + weight * (direction.X - head.X), head.Y + weight * (direction.Y - head.Y));
                state.Head = head;

                float strength = MathF.Sqrt(head.X * head.X + head.Y * head.Y);
                if (strength >= HeadConfidence)
                {
                    state.HeadDirection = new Point2f(head.X / strength, head.Y / strength);
                }
                return;
            }

            // Berputar di tempat: sisi panjang bbox utuh jelas tegak lurus sumbu lama
            if (state.Direction is Point2f d && !IsCutByFrame(box, frameSize) &&
                Math.Max(box.Width, box.Height) >= TurnAspectRatio * Math.Min(box.Width, box.Height) &&
                Math.Abs(d.X) >= Math.Abs(d.Y) != box.Width >= box.Height)
            {
                ResetEwe(state);
            }
        }

        private static bool IsCutByFrame(Rect box, Size frameSize)
        {
            return box.Left <= 0 || box.Top <= 0 || box.Right >= frameSize.Width - 1 || box.Bottom >= frameSize.Height - 1;
        }

        private static void ResetEwe(EweState state)
        {
            state.History.Clear();
            state.Direction = null;
            state.Head = null;
            state.HeadDirection = null;
        }

        private bool IsMoving(Queue<(double Time, Point2f Centroid)> history, float shortSide)
        {
            var (oldestTime, oldest) = history.Peek();
            var (newestTime, newest) = history.Last();
            double elapsed = newestTime - oldestTime;

            // Butuh rentang waktu yang cukup agar getaran bbox tidak dianggap berjalan
            if (elapsed < lambMovingWindowSeconds / 2)
            {
                return false;
            }

            return CentroidTrackerService.Distance(oldest, newest) / elapsed >= lambMinimumSpeed * shortSide;
        }

        private static void UpdateHistory(Queue<(double Time, Point2f Centroid)> history, double timeSeconds, Point2f centroid,
            double windowSeconds)
        {
            history.Enqueue((timeSeconds, centroid));
            while (history.Peek().Time < timeSeconds - windowSeconds)
            {
                history.Dequeue();
            }
        }

        private float Blend(float? baseline, float value)
        {
            return baseline.HasValue ? baseline.Value + baselineWeight * (value - baseline.Value) : value;
        }

        private static bool HasOverlap(Rect a, Rect b)
        {
            return a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
        }

        private static bool Contains(Rect zone, Point2f point)
        {
            return point.X >= zone.X && point.X < zone.X + zone.Width && point.Y >= zone.Y && point.Y < zone.Y + zone.Height;
        }

        private static T GetState<T>(Dictionary<int, T> states, int trackId) where T : new()
        {
            if (!states.TryGetValue(trackId, out var state))
            {
                state = new T();
                states[trackId] = state;
            }

            return state;
        }

        private class EweState
        {
            public Queue<(double Time, Point2f Centroid)> History { get; } = new();
            public Point2f? Direction { get; set; }
            // Rata-rata arah gerak, dan arah kepala terakhir yang yakin (vektor satuan)
            public Point2f? Head { get; set; }
            public Point2f? HeadDirection { get; set; }
            public bool IsHorizontal { get; set; }
            public double LastSeenSeconds { get; set; }
        }

        private class LambState
        {
            public Queue<(double Time, Point2f Centroid)> History { get; } = new();
            public float? BaselineRatio { get; set; }
            public float? BaselineArea { get; set; }
            public double LastSeenSeconds { get; set; }
            public double LastVisibleSeconds { get; set; }
            public bool WasInUdderZone { get; set; }
            public bool WasStanding { get; set; }
            public bool WasShrunk { get; set; }

            public SucklingEvent Approach { get; set; }
            public double ApproachStartSeconds { get; set; }
            public double ApproachLastSeconds { get; set; }

            public SucklingEvent Stay { get; set; }
            public double StayStartSeconds { get; set; }
            public double StayLastSeconds { get; set; }
        }
    }
}
