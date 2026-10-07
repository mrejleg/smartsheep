using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Project.Core;
using Web.ApplicationCore.Dis;
using Web.ApplicationCore.Interfaces.Dashboards;
using Web.ApplicationCore.Services.Dashboards;

namespace Web.ApplicationCore
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationCores(this IServiceCollection services)
        {
            services.AddCores();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddAuthServices();

            services.AddConfigsServices();

            services.AddMasterDataServices();

            services.AddSmartSheepServices();

            services.AddNotificationServices();
            services.AddScoped<IDashboardService, DashboardService>();


            return services;
        }
    }
}
