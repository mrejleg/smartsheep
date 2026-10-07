using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface ICentroidTrackerService
    {
        List<TrackedObject> Update(List<Detection> detections);
    }
}
