using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Interfaces.MasterDatas
{
    public interface ISourceVideoStreamService
    {
        string GetEmbedUrl(string sourceVideoUrl);
        string GetInput(SourceVideo sourceVideo);
        Task WriteFrameAsync(Stream output, byte[] jpeg, CancellationToken cancellationToken);
    }
}
