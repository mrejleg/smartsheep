using Api.ApplicationCore.Dis;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.ApplicationCore.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Core;

namespace Api.ApplicationCore
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationCores(this IServiceCollection services)
        {
            services.AddCores();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddAuthServices();

            services.AddConfigServices();

            services.AddApiManagementServices();

            services.AddMasterDataServices();

            services.AddSmartSheepServices();

            services.AddTransient<ISystemNotificationRepositories, SystemNotificationRepositories>();

            return services;
        }
    }
}
