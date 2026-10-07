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
    public class AppClientService : IAppClientService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public AppClientService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<AppClient>(_client, "AppClient/DxGrid", param);
        }

        public async Task<RestApiResult<List<AppClient>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppClient>>(_client, "AppClient/All");
        }

        public async Task<RestApiResult<List<AppClient>>> GetAll()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppClient>>(_client, "AppClient/GetAll");
        }

        public async Task<RestApiResult<AppClient>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppClient>(_client, "AppClient/" + id);
        }

        public async Task<RestApiResult<AppClient>> GetAsync(string clientId)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppClient>(_client, "AppClient/Client/" + clientId);
        }

        public async Task<RestApiResult<AppClient>> PostAsync(AppClient param)
        {
            return await HttpClientExtentions.SecuredPostAsync<AppClient>(_client, "AppClient", param);
        }

        public async Task<RestApiResult<AppClient>> PutAsync(AppClient param)
        {
            return await HttpClientExtentions.SecuredPutAsync<AppClient>(_client, "AppClient", param);
        }

        public async Task<RestApiResult<AppClient>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<AppClient>(_client, "AppClient/" + id);
        }

        public async Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<DxDropDownBoxBinding>>(_client, "AppClient/Binding/DxDropDownBox");
        }
    }
}
