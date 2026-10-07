using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Configs
{
    [Authorize(Policy = "Bearer")]
    public class SystemConfigService : ISystemConfigService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public SystemConfigService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<SystemConfig>(_client, "SystemConfig/DxGrid", param);
        }

        public async Task<RestApiResult<List<SystemConfig>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SystemConfig>>(_client, "SystemConfig/All");
        }

        public async Task<RestApiResult<SystemConfig>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<SystemConfig>(_client, "SystemConfig/" + id);
        }

        public async Task<RestApiResult<SystemConfig>> PostAsync(SystemConfig model)
        {
            return await HttpClientExtentions.SecuredPostAsync<SystemConfig>(_client, "SystemConfig", model);
        }

        public async Task<RestApiResult<SystemConfig>> PutAsync(SystemConfig model)
        {
            return await HttpClientExtentions.SecuredPutAsync<SystemConfig>(_client, "SystemConfig", model);
        }

        public async Task<RestApiResult<SystemConfig>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<SystemConfig>(_client, "SystemConfig/" + id);
        }
    }
}
