using Extractor.Domain.Models.Extractors;

namespace Extractor.ApplicationCore.Interfaces.Extractors
{
    public interface IFrameExtractorService
    {
        List<string> GetVideos(string input);
        string GetOutputFolder(string input, string video, string output);
        bool IsExtracted(string outputFolder);
        ExtractResult Extract(string video, string outputFolder, ExtractorOption option);
    }
}
