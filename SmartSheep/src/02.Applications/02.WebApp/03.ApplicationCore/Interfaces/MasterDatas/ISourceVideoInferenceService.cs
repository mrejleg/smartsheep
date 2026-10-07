using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Interfaces.MasterDatas
{
    public interface ISourceVideoInferenceService
    {
        Task WriteFramesAsync(SourceVideo sourceVideo, Guid previewId, Stream output, CancellationToken cancellationToken);
        SourceVideoInferenceStatistic GetStatistic(Guid previewId);
    }
}
