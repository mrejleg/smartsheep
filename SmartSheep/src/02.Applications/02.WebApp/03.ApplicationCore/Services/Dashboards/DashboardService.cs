using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Dashboards;
using Web.Domain.Models;
using Web.Domain.Models.Dashboards;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Dashboards
{
    public class DashboardService : IDashboardService
    {
        private readonly HttpClient _client;

        public DashboardService(AppSetting appSettings)
        {
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(appSettings));
        }

        public Task<RestApiResult<DashboardDataModel>> GetAsync(DateTime? startDate, DateTime? endDate, Guid? farmId, Guid? barnId)
        {
            var parameters = new List<string>();
            if (startDate.HasValue) parameters.Add($"startDate={Uri.EscapeDataString(startDate.Value.ToString("yyyy-MM-dd"))}");
            if (endDate.HasValue) parameters.Add($"endDate={Uri.EscapeDataString(endDate.Value.ToString("yyyy-MM-dd"))}");
            if (farmId.HasValue) parameters.Add($"farmId={farmId.Value}");
            if (barnId.HasValue) parameters.Add($"barnId={barnId.Value}");
            var endpoint = "SmartSheepReport/Dashboard" + (parameters.Count > 0 ? $"?{string.Join("&", parameters)}" : string.Empty);
            return HttpClientExtentions.SecuredGetAsync<DashboardDataModel>(_client, endpoint);
        }
    }
}
