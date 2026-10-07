using InferenceYolo.Models.Inferences;
using OpenCvSharp;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface IFrameAnnotatorService
    {
        void Draw(Mat frame, FrameAnalysis analysis);
    }
}
