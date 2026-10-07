using Extractor.ApplicationCore.Interfaces.Extractors;
using Extractor.ApplicationCore.Services.Extractors;
using Microsoft.Extensions.DependencyInjection;

namespace Extractor.ApplicationCore.Dis
{
    public static class ExtractorDi
    {
        public static IServiceCollection AddExtractorServices(this IServiceCollection services)
        {
            services.AddTransient<IFrameExtractorService, FrameExtractorService>();
            return services;
        }
    }
}
