using Api.Domain.Entities.SmartSheep;
using Api.Domain.Models.Dashboards;
using Api.Domain.Models.Reports;

namespace Api.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISmartSheepReportService
    {
        Task<DashboardDataModel> GetDashboardAsync(DashboardFilterParam param);
        IQueryable<SucklingActivity> GetSucklingActivities(SmartSheepReportFilterParam param);
        IQueryable<SucklingStatistic> GetSucklingStatistics(SmartSheepReportFilterParam param);
        IQueryable<ReminderLog> GetReminders(SmartSheepReportFilterParam param);
    }
}
