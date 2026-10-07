using Api.ApplicationCore.Interfaces.Repositories;
using Api.ApplicationCore.Interfaces.SmartSheep;
using Api.ApplicationCore.Services.Repositories;
using Api.ApplicationCore.Services.SmartSheep;
using Microsoft.Extensions.DependencyInjection;

namespace Api.ApplicationCore.Dis
{
    public static class SmartSheepDi
    {
        public static IServiceCollection AddSmartSheepServices(this IServiceCollection services)
        {
            services.AddTransient<ISucklingActivityService, SucklingActivityService>();
            services.AddTransient<ISucklingStatisticService, SucklingStatisticService>();
            services.AddTransient<ISourceVideoStatusLogService, SourceVideoStatusLogService>();
            services.AddTransient<IJobExecutionLogService, JobExecutionLogService>();
            services.AddTransient<IReminderLogService, ReminderLogService>();
            services.AddTransient<ISmartSheepReportService, SmartSheepReportService>();
            services.AddTransient<ISmartSheepRepositories, SmartSheepRepositories>();
            return services;
        }
    }
}
