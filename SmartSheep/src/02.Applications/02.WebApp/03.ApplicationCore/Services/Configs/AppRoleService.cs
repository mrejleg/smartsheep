using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
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
    public class AppRoleService : IAppRoleService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;


        public AppRoleService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }


        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<AppRole>(_client, "AppRole/DxGrid", param);
        }


        public async Task<RestApiResult<List<AppRole>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppRole>>(_client, "AppRole/All");
        }


        public async Task<RestApiResult<List<AppRole>>> GetAllAsync(int id)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppRole>>(_client, "AppRole/All/" + id);
        }


        public async Task<RestApiResult<AppRole>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppRole>(_client, "AppRole/" + id);
        }

        public async Task<RestApiResult<AppRole>> GetAsync(string name)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppRole>(_client, "AppRole/Name/" + name);
        }


        public async Task<RestApiResult<AppRole>> PostAsync(AppRole param)
        {
            return await HttpClientExtentions.SecuredPostAsync<AppRole>(_client, "AppRole", param);
        }


        public async Task<RestApiResult<AppRole>> PutAsync(AppRole param)
        {
            return await HttpClientExtentions.SecuredPutAsync<AppRole>(_client, "AppRole", param);
        }


        public async Task<RestApiResult<AppRole>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<AppRole>(_client, "AppRole/" + id);
        }


        public async Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<DxDropDownBoxBinding>>(_client, "AppRole/Binding/DxDropDownBox");
        }
    }
}
