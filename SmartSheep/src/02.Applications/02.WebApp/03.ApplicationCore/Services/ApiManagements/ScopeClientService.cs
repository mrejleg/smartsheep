using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
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
    public class ScopeClientService : IScopeClientService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public ScopeClientService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<ScopeClient>(_client, "ScopeClient/DxGrid", param);
        }

        public async Task<RestApiResult<List<ScopeClient>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<ScopeClient>>(_client, "ScopeClient/All");
        }

        public async Task<RestApiResult<ScopeClient>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<ScopeClient>(_client, "ScopeClient/" + id);
        }

        public async Task<RestApiResult<ScopeClient>> GetAsync(string key)
        {
            return await HttpClientExtentions.SecuredGetAsync<ScopeClient>(_client, "ScopeClient/Name/" + key);
        }

        public async Task<RestApiResult<ScopeClient>> PostAsync(ScopeClient param)
        {
            return await HttpClientExtentions.SecuredPostAsync<ScopeClient>(_client, "ScopeClient", param);
        }

        public async Task<RestApiResult<ScopeClient>> PutAsync(ScopeClient param)
        {
            return await HttpClientExtentions.SecuredPutAsync<ScopeClient>(_client, "ScopeClient", param);
        }

        public async Task<RestApiResult<ScopeClient>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<ScopeClient>(_client, "ScopeClient/" + id);
        }

        public async Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<DxDropDownBoxBinding>>(_client, "ScopeClient/Binding/DxDropDownBox");
        }
    }
}
