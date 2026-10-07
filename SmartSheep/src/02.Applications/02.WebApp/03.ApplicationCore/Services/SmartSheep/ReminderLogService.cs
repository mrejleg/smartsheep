using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.SmartSheep;
using Web.Domain.Models;
using Web.Domain.Models.SmartSheep;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.SmartSheep
{
    [Authorize(Policy = "Bearer")]
    public class ReminderLogService : IReminderLogService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public ReminderLogService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<ReminderLog>(_client, "ReminderLog/DxGrid", param);
        }

        public async Task<RestApiResult<List<ReminderLog>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<ReminderLog>>(_client, "ReminderLog/All");
        }

        public async Task<RestApiResult<List<ReminderLog>>> GetAllByReminderTemplateAsync(Guid fkReminderTemplateId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<ReminderLog>>(_client, "ReminderLog/GetAllByReminderTemplate/" + fkReminderTemplateId);
        }

        public async Task<RestApiResult<List<ReminderLog>>> GetAllBySucklingStatisticAsync(Guid fkSucklingStatisticId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<ReminderLog>>(_client, "ReminderLog/GetAllBySucklingStatistic/" + fkSucklingStatisticId);
        }

        public async Task<RestApiResult<ReminderLog>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<ReminderLog>(_client, "ReminderLog/" + id);
        }

        public async Task<RestApiResult<ReminderLog>> PostAsync(ReminderLog model)
        {
            return await HttpClientExtentions.SecuredPostAsync<ReminderLog>(_client, "ReminderLog", model);
        }

        public async Task<RestApiResult<ReminderLog>> PutAsync(ReminderLog model)
        {
            return await HttpClientExtentions.SecuredPutAsync<ReminderLog>(_client, "ReminderLog", model);
        }

        public async Task<RestApiResult<ReminderLog>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<ReminderLog>(_client, "ReminderLog/" + id);
        }
    }
}
