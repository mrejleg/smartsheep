using Project.Base.Models.RestApis;
using Web.Domain.Models.Dashboards;

namespace Web.ApplicationCore.Interfaces.Dashboards
{
    public interface IDashboardService
    {
        Task<RestApiResult<DashboardDataModel>> GetAsync(DateTime? startDate, DateTime? endDate, Guid? farmId, Guid? barnId);
    }
}
