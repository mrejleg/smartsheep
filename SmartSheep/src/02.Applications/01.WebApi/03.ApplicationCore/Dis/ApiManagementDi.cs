using Api.ApplicationCore.Interfaces.ApiManagements;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.ApplicationCore.Services.ApiManagements;
using Api.ApplicationCore.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Api.ApplicationCore.Dis
{
    public static class ApiManagementDi
    {
        public static IServiceCollection AddApiManagementServices(this IServiceCollection services)
        {
            services.AddTransient<IScopeClientService, ScopeClientService>();
            services.AddTransient<IAppClientService, AppClientService>();
            services.AddTransient<IAppScopeClientService, AppScopeClientService>();

            //Unit of Works
            services.AddTransient<IApiManagementRepositories, ApiManagementRepositories>();

            return services;
        }
    }
}
