using Api.ApplicationCore.Interfaces.Configs;
using Api.ApplicationCore.Interfaces.Repositories;
using Api.ApplicationCore.Services.Configs;
using Api.ApplicationCore.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Api.ApplicationCore.Dis
{
    public static class ConfigDi
    {
        public static IServiceCollection AddConfigServices(this IServiceCollection services)
        {
            services.AddTransient<IAppRoleService, AppRoleService>();
            services.AddTransient<IAppMenuService, AppMenuService>();
            services.AddTransient<IAppMenuRoleService, AppMenuRoleService>();
            services.AddTransient<IEmailTemplateService, EmailTemplateService>();
            services.AddTransient<ISystemConfigService, SystemConfigService>();
            services.AddTransient<IReminderTemplateService, ReminderTemplateService>();
            services.AddTransient<INewsService, NewsService>();

            //Unit of Works
            services.AddTransient<IConfigRepositories, ConfigRepositories>();

            return services;
        }
    }
}
