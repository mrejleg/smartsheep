using Microsoft.AspNetCore.Http;
using Web.ApplicationCore.Interfaces.Repositories;
using Web.ApplicationCore.Interfaces.SmartSheep;
using Web.ApplicationCore.Services.SmartSheep;
using Web.Domain.Models;

namespace Web.ApplicationCore.Services.Repositories
{
    public class SmartSheepRepositories : ISmartSheepRepositories
    {
        public AppSetting AppSettings { get; private set; }
        public IHttpContextAccessor HttpContextAccessors { get; private set; }
        public ISucklingActivityService SucklingActivities { get; private set; }
        public ISucklingStatisticService SucklingStatistics { get; private set; }
        public ISourceVideoStatusLogService SourceVideoStatusLogs { get; private set; }
        public IJobExecutionLogService JobExecutionLogs { get; private set; }
        public IReminderLogService ReminderLogs { get; private set; }

        private readonly AppSetting _appSetting;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SmartSheepRepositories(AppSetting appSetting, IHttpContextAccessor httpContextAccessor)
        {
            _appSetting = appSetting;
            _httpContextAccessor = httpContextAccessor;
            AppSettings = _appSetting;
            HttpContextAccessors = new HttpContextAccessor();
            SucklingActivities = new SucklingActivityService(_appSetting);
            SucklingStatistics = new SucklingStatisticService(_appSetting);
            SourceVideoStatusLogs = new SourceVideoStatusLogService(_appSetting);
            JobExecutionLogs = new JobExecutionLogService(_appSetting);
            ReminderLogs = new ReminderLogService(_appSetting);
        }
    }
}
