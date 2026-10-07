using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.SmartSheep;
using Web.Domain.Models;

namespace Web.ApplicationCore.Interfaces.Repositories
{
    public interface ISmartSheepRepositories
    {
        AppSetting AppSettings { get; }
        IHttpContextAccessor HttpContextAccessors { get; }
        ISucklingActivityService SucklingActivities { get; }
        ISucklingStatisticService SucklingStatistics { get; }
        ISourceVideoStatusLogService SourceVideoStatusLogs { get; }
        IJobExecutionLogService JobExecutionLogs { get; }
        IReminderLogService ReminderLogs { get; }
    }
}
