using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface ISucklingStatisticService
    {
        SucklingStatistic Calculate(string sourceVideoCode, List<SucklingEvent> sucklingEvents, DateTime from, DateTime to);
    }
}
