using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.ApiManagements;
using Web.Domain.Models;
using Web.Domain.Models.ApiManagements;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.ApiManagements
{
    [Authorize(Policy = "Bearer")]
    public class AppScopeClientService : IAppScopeClientService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public AppScopeClientService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<AppScopeClient>(_client, "AppScopeClient/DxGrid", param);
        }

        public async Task<RestApiResult<List<AppScopeClient>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppScopeClient>>(_client, "AppScopeClient/All");
        }

        public async Task<RestApiResult<List<AppScopeClient>>> GetAllAsync(Guid fkAppClientId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppScopeClient>>(_client, "AppScopeClient/All/" + fkAppClientId);
        }

        public async Task<RestApiResult<AppScopeClient>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppScopeClient>(_client, "AppScopeClient/" + id);
        }

        public async Task<RestApiResult<AppScopeClient>> PostAsync(AppScopeClient param)
        {
            return await HttpClientExtentions.SecuredPostAsync<AppScopeClient>(_client, "AppScopeClient", param);
        }

        public async Task<RestApiResult<AppScopeClient>> PutAsync(AppScopeClient param)
        {
            return await HttpClientExtentions.SecuredPutAsync<AppScopeClient>(_client, "AppScopeClient", param);
        }

        public async Task<RestApiResult<AppScopeClient>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<AppScopeClient>(_client, "AppScopeClient/" + id);
        }

        public async Task<RestApiResult<List<AppScopeClient>>> GetAllByAppClientAsync(Guid fkAppClientId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppScopeClient>>(_client, "AppScopeClient/GetAllByAppClient/" + fkAppClientId);
        }

        public async Task<RestApiResult<List<AppScopeClient>>> GetAllByScopeClientAsync(Guid fkScopeClientId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppScopeClient>>(_client, "AppScopeClient/GetAllByScopeClient/" + fkScopeClientId);
        }

        public async Task<RestApiResult<object>> ReplaceAsync(Guid fkAppClientId, IReadOnlyCollection<AppScopeClient> models)
        {
            return await HttpClientExtentions.SecuredPostAsync<object>(_client, "AppScopeClient/Replace/" + fkAppClientId, models);
        }

    }
}
