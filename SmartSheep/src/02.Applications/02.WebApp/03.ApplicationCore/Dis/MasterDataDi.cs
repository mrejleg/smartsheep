using Microsoft.Extensions.DependencyInjection;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.MasterDatas;
using Web.ApplicationCore.Services.Repositories;

namespace Web.ApplicationCore.Dis
{
    public static class MasterDataDi
    {
        public static IServiceCollection AddMasterDataServices(this IServiceCollection services)
        {
            services.AddTransient<IFarmService, FarmService>();
            services.AddTransient<IBarnService, BarnService>();
            services.AddTransient<ILivestockService, LivestockService>();
            services.AddTransient<ISourceVideoService, SourceVideoService>();
            services.AddTransient<ISourceVideoStreamService, SourceVideoStreamService>();
            services.AddSingleton<ISourceVideoInferenceService, SourceVideoInferenceService>();
            services.AddTransient<IMasterDataRepositories, MasterDataRepositories>();
            return services;
        }
    }
}
