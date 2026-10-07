using OpenCvSharp;

namespace InferenceYolo.Models.Inferences
{
    public class Detection
    {
        public int ClassId { get; set; }
        public string Label { get; set; } = "";
        public float Confidence { get; set; }
        public Rect Box { get; set; }
    }
}
