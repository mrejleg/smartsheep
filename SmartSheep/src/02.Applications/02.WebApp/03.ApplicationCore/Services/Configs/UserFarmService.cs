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
    public class UserFarmService : IUserFarmService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public UserFarmService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<UserFarm>(_client, "UserFarm/DxGrid", param);
        }

        public async Task<RestApiResult<UserFarmOptions>> GetOptionsAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<UserFarmOptions>(_client, "UserFarm/Options");
        }

        public async Task<RestApiResult<UserFarm>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<UserFarm>(_client, "UserFarm/" + id);
        }

        public async Task<RestApiResult<UserFarm>> PostAsync(UserFarm model)
        {
            return await HttpClientExtentions.SecuredPostAsync<UserFarm>(_client, "UserFarm", model);
        }

        public async Task<RestApiResult<UserFarm>> PutAsync(UserFarm model)
        {
            return await HttpClientExtentions.SecuredPutAsync<UserFarm>(_client, "UserFarm", model);
        }

        public async Task<RestApiResult<UserFarm>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<UserFarm>(_client, "UserFarm/" + id);
        }
    }
}
