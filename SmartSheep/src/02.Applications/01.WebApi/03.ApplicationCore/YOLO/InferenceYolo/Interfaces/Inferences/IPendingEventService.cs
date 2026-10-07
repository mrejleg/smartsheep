using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface IPendingEventService
    {
        void Save(SucklingEvent sucklingEvent);
        void SyncIfIdle(DateTime frameAt, double timeSeconds, bool hasActiveSuckling);
        int SyncAll(DateTime? processedUntil);
    }
}
