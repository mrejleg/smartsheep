using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.ApplicationCore.Interfaces.ApiManagements;
using Web.ApplicationCore.Interfaces.Configs;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Services.ApiManagements;
using Web.ApplicationCore.Services.Configs;
using Web.ApplicationCore.Services.Repositories;

namespace Web.ApplicationCore.Dis
{
    public static class ConfigsDi
    {
        public static IServiceCollection AddConfigsServices(this IServiceCollection services)
        {
            // API clients contain per-user authorization headers, so their owners
            // must not survive beyond the current request.
            services.AddScoped<IAppClientService, AppClientService>();
            services.AddScoped<IScopeClientService, ScopeClientService>();
            services.AddScoped<IAppScopeClientService, AppScopeClientService>();
            services.AddScoped<IAppRoleService, AppRoleService>();
            services.AddScoped<IAppMenuService, AppMenuService>();
            services.AddScoped<IAppMenuRoleService, AppMenuRoleService>();
            services.AddScoped<ISystemConfigService, SystemConfigService>();
            services.AddScoped<IReminderTemplateService, ReminderTemplateService>();
            services.AddScoped<INewsService, NewsService>();
            services.AddScoped<IUserFarmService, UserFarmService>();
            //Unit of Works
            services.AddTransient<IConfigRepositories, ConfigsRepositories>();
            return services;
        }
    }
}
