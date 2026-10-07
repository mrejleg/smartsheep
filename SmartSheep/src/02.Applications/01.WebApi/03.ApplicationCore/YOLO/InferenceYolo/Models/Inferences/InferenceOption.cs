namespace InferenceYolo.Models.Inferences
{
    public class InferenceOption
    {
        public string Input { get; set; } = "";                  // file video, folder rekaman, atau URL live streaming; kosong = URL dari SourceVideo
        public string SourceVideoCode { get; set; } = "";        // kode SourceVideo; default dari nama file/folder
        public string BarnCode { get; set; } = "";               // kode kandang (FK Barn di SourceVideo), folder pending per kandang
        public string Output { get; set; } = "output";
        public string Model { get; set; } = Path.Combine("..", "smartsheep.onnx");
        public string Labels { get; set; } = Path.Combine("..", "smartsheep.labels.json");
        public string AppSettings { get; set; } = Path.Combine("..", "..", "..", "04.Api", "appsettings.json");
        public bool SaveVideo { get; set; } = true;
        public bool Trace { get; set; }
        public bool UseCoreMl { get; set; }
        public bool IsOffline { get; set; }                       // tulis kejadian ke CSV, tanpa sync ke API
        public bool IsService { get; set; }                       // worker background untuk semua SourceVideo online
        public int? MaxFrames { get; set; }

        public void CopyTo(InferenceOption target)
        {
            target.Input = Input;
            target.SourceVideoCode = SourceVideoCode;
            target.BarnCode = BarnCode;
            target.Output = Output;
            target.Model = Model;
            target.Labels = Labels;
            target.AppSettings = AppSettings;
            target.SaveVideo = SaveVideo;
            target.Trace = Trace;
            target.UseCoreMl = UseCoreMl;
            target.IsOffline = IsOffline;
            target.IsService = IsService;
            target.MaxFrames = MaxFrames;
        }
    }
}
