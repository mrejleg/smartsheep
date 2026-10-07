using InferenceYolo.Models.Inferences;
using OpenCvSharp;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface ISucklingAnalyzerService
    {
        FrameAnalysis Analyze(List<TrackedObject> objects, double timeSeconds, DateTime frameAt, Size frameSize);
        List<SucklingEvent> Complete();
    }
}
