namespace InferenceYolo.Models.Inferences
{
    // SourceVideo dari API beserta kandangnya; URL sudah memuat username/password kamera bila ada.
    public class SyncSource
    {
        public string Code { get; set; } = "";
        public string BarnCode { get; set; } = "";
        public string SourceVideoUrl { get; set; } = "";
    }
}
