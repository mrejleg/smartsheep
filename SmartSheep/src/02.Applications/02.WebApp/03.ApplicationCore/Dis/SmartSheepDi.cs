using Microsoft.Extensions.DependencyInjection;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Interfaces.SmartSheep;
using Web.ApplicationCore.Services.Repositories;
using Web.ApplicationCore.Services.SmartSheep;

namespace Web.ApplicationCore.Dis
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
