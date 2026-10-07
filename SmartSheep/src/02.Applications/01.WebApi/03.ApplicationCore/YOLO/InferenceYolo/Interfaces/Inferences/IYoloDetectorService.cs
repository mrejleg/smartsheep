using InferenceYolo.Models.Inferences;
using OpenCvSharp;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface IYoloDetectorService : IDisposable
    {
        int InputSize { get; }
        IReadOnlyDictionary<int, string> Labels { get; }
        List<Detection> Detect(Mat frame);
    }
}
