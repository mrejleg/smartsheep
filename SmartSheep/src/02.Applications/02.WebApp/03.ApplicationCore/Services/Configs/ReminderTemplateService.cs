using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.Firebases;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Configs
{
    [Authorize(Policy = "Bearer")]
    public class ReminderTemplateService : IReminderTemplateService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public ReminderTemplateService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<ReminderTemplate>(_client, "ReminderTemplate/DxGrid", param);
        }

        public async Task<RestApiResult<List<ReminderTemplate>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<ReminderTemplate>>(_client, "ReminderTemplate/All");
        }

        public async Task<RestApiResult<ReminderTemplate>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<ReminderTemplate>(_client, "ReminderTemplate/" + id);
        }

        public async Task<RestApiResult<ReminderTemplate>> PostAsync(ReminderTemplate model)
        {
            return await HttpClientExtentions.SecuredPostAsync<ReminderTemplate>(_client, "ReminderTemplate", model);
        }

        public async Task<RestApiResult<ReminderTemplate>> PutAsync(ReminderTemplate model)
        {
            return await HttpClientExtentions.SecuredPutAsync<ReminderTemplate>(_client, "ReminderTemplate", model);
        }

        public async Task<RestApiResult<ReminderTestResultDto>> TestAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredPostAsync<ReminderTestResultDto>(
                _client,
                $"ReminderTemplate/{id}/Test",
                new { });
        }

        public async Task<RestApiResult<ReminderTemplate>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<ReminderTemplate>(_client, "ReminderTemplate/" + id);
        }
    }
}
