using InferenceYolo.Models.Inferences;

namespace InferenceYolo.Interfaces.Inferences
{
    public interface IInferenceRunnerService
    {
        SucklingStatistic Run(CancellationToken cancellationToken);
    }
}
