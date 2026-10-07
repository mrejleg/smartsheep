using Api.ApplicationCore.Interfaces.MasterDatas;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.ApplicationCore.Services.MasterDatas;
using Api.ApplicationCore.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Api.ApplicationCore.Dis
{
    public static class MasterDataDi
    {
        public static IServiceCollection AddMasterDataServices(this IServiceCollection services)
        {
            services.AddTransient<IFarmService, FarmService>();
            services.AddTransient<IBarnService, BarnService>();
            services.AddTransient<ILivestockService, LivestockService>();
            services.AddTransient<ISourceVideoService, SourceVideoService>();
            services.AddTransient<IMasterDataRepositories, MasterDataRepositories>();
            return services;
        }
    }
}
