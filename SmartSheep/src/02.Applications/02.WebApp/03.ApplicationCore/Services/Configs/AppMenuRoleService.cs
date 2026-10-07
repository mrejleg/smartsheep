using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Auths;
using Web.Domain.Models.Configs;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Configs
{
    [Authorize(Policy = "Bearer")]
    public class AppMenuRoleService : IAppMenuRoleService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;


        public AppMenuRoleService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }


        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<AppMenuRole>(_client, "AppMenuRole/DxGrid", param);
        }


        public async Task<RestApiResult<List<AppMenuRole>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenuRole>>(_client, "AppMenuRole/All");
        }


        public async Task<RestApiResult<List<AppMenuRole>>> GetAllAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenuRole>>(_client, "AppMenuRole/All/" + id);
        }


        public async Task<RestApiResult<AppMenuRole>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppMenuRole>(_client, "AppMenuRole/" + id);
        }


        public async Task<RestApiResult<AppMenuRole>> PostAsync(AppMenuRole param)
        {
            return await HttpClientExtentions.SecuredPostAsync<AppMenuRole>(_client, "AppMenuRole", param);
        }


        public async Task<RestApiResult<AppMenuRole>> PutAsync(AppMenuRole param)
        {
            return await HttpClientExtentions.SecuredPutAsync<AppMenuRole>(_client, "AppMenuRole", param);
        }


        public async Task<RestApiResult<AppMenuRole>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<AppMenuRole>(_client, "AppMenuRole/" + id);
        }


        public async Task<RestApiResult<List<AppMenuRole>>> GetAllByAppRoleAsync(Guid fkAppRoleId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenuRole>>(_client, "AppMenuRole/GetAllByAppRole/" + fkAppRoleId);
        }


        public async Task<RestApiResult<List<AppMenuRole>>> GetAllByAppMenuAsync(Guid fkAppMenuId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenuRole>>(_client, "AppMenuRole/GetAllByAppMenu/" + fkAppMenuId);
        }


        public async Task<RestApiResult<object>> ReplaceAsync(Guid fkAppRoleId, IReadOnlyCollection<AppMenuRole> models)
        {
            return await HttpClientExtentions.SecuredPostAsync<object>(_client, "AppMenuRole/Replace/" + fkAppRoleId, models);
        }
    }
}
