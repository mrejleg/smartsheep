using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.SmartSheep;
using Web.Domain.Models;
using Web.Domain.Models.SmartSheep;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.SmartSheep
{
    public class SmartSheepReportService : ISmartSheepReportService
    {
        private readonly HttpClient _client;

        public SmartSheepReportService(AppSetting appSettings)
        {
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(appSettings));
        }

        public Task<RestApiResult<List<SucklingActivity>>> GetSucklingActivitiesAsync(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize)
        {
            return HttpClientExtentions.SecuredGetAsync<List<SucklingActivity>>(_client, BuildEndpoint("SucklingActivities", from, to, farmId, barnId, page, pageSize));
        }

        public Task<RestApiResult<List<SucklingStatistic>>> GetSucklingStatisticsAsync(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize)
        {
            return HttpClientExtentions.SecuredGetAsync<List<SucklingStatistic>>(_client, BuildEndpoint("SucklingStatistics", from, to, farmId, barnId, page, pageSize));
        }

        public Task<RestApiResult<List<ReminderLog>>> GetRemindersAsync(DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize)
        {
            return HttpClientExtentions.SecuredGetAsync<List<ReminderLog>>(_client, BuildEndpoint("Reminders", from, to, farmId, barnId, page, pageSize));
        }

        private static string BuildEndpoint(string route, DateTime? from, DateTime? to, Guid? farmId, Guid? barnId, int page, int pageSize)
        {
            var parameters = new List<string>();
            if (from.HasValue) parameters.Add("from=" + Uri.EscapeDataString(from.Value.ToString("O")));
            if (to.HasValue) parameters.Add("to=" + Uri.EscapeDataString(to.Value.ToString("O")));
            if (farmId.HasValue) parameters.Add("farmId=" + farmId.Value);
            if (barnId.HasValue) parameters.Add("barnId=" + barnId.Value);
            parameters.Add("page=" + Math.Max(page, 1));
            parameters.Add("pageSize=" + Math.Clamp(pageSize, 1, 100));
            return "SmartSheepReport/" + route + (parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty);
        }
    }
}
