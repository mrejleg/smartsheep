using OpenCvSharp;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface IVideoSourceService : IDisposable
    {
        bool IsStream { get; }
        int VideoCount { get; }
        int ReconnectCount { get; }
        double Fps { get; }
        int FrameCount { get; }
        Size FrameSize { get; }
        DateTime StartedAt { get; }
        void Open(string input);
        bool Read(Mat frame, out double timeSeconds);
        void Stop();
    }
}
