using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface ISucklingSyncService
    {
        bool Sync(string sourceVideoCode, DateTime processedFrom, DateTime processedUntil, List<SucklingEvent> sucklingEvents);
        List<SyncSource> GetSources();
        SyncSource GetSource(string sourceVideoCode);
        bool ReadSystemConfig(AppSetting setting);
        bool ReportStatus(string sourceVideoCode, bool isOnline);
    }
}
