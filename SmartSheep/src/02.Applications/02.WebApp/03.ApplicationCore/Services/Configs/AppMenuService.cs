using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Select2s;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.Configs;
using Web.Domain.Models;
using Web.Domain.Models.Configs;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.Configs
{
    [Authorize(Policy = "Bearer")]
    public class AppMenuService : IAppMenuService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;


        public AppMenuService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }


        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<AppMenu>(_client, "AppMenu/DxGrid", param);
        }


        public async Task<RestApiResult<List<AppMenu>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenu>>(_client, "AppMenu/All");
        }


        public async Task<RestApiResult<List<AppMenu>>> GetAllAsync(int id)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenu>>(_client, "AppMenu/All/" + id);
        }

        public async Task<RestApiResult<List<AppMenu>>> GetAllByParentAsync(Guid fkParentId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<AppMenu>>(_client, "AppMenu/GetAllByParent/" + fkParentId);
        }


        public async Task<RestApiResult<AppMenu>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<AppMenu>(_client, "AppMenu/" + id);
        }


        public async Task<RestApiResult<AppMenu>> PostAsync(AppMenu param)
        {
            return await HttpClientExtentions.SecuredPostAsync<AppMenu>(_client, "AppMenu", param);
        }


        public async Task<RestApiResult<AppMenu>> PutAsync(AppMenu param)
        {
            return await HttpClientExtentions.SecuredPutAsync<AppMenu>(_client, "AppMenu", param);
        }


        public async Task<RestApiResult<AppMenu>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<AppMenu>(_client, "AppMenu/" + id);
        }


        public async Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<DxDropDownBoxBinding>>(_client, "AppMenu/Binding/DxDropDownBox/Parent");
        }


        public async Task<RestApiResult<List<Select2Binding>>> BindingSelect2AccessTypes()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<Select2Binding>>(_client, "AppMenu/Binding/Select2/AccessType");
        }
    }
}
