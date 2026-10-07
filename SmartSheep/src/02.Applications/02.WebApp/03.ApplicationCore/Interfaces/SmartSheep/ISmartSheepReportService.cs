using Project.Base.Models.RestApis;
using Web.Domain.Models.SmartSheep;

namespace Web.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISmartSheepReportService
    {
        Task<RestApiResult<List<SucklingActivity>>> GetSucklingActivitiesAsync(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize);
        Task<RestApiResult<List<SucklingStatistic>>> GetSucklingStatisticsAsync(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize);
        Task<RestApiResult<List<ReminderLog>>> GetRemindersAsync(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize);
    }
}
