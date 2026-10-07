using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface ISucklingSyncService
    {
        bool Sync(string sourceVideoCode, DateTime processedFrom, DateTime processedUntil, List<SucklingEvent> sucklingEvents);
        List<SyncSource> GetSources();
        SyncSource GetSource(string sourceVideoCode);
    }
}
