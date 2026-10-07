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
    public class SucklingActivityService : ISucklingActivityService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public SucklingActivityService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<SucklingActivity>(_client, "SucklingActivity/DxGrid", param);
        }

        public async Task<RestApiResult<List<SucklingActivity>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SucklingActivity>>(_client, "SucklingActivity/All");
        }

        public async Task<RestApiResult<List<SucklingActivity>>> GetAllByBarnAsync(Guid fkBarnId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SucklingActivity>>(_client, "SucklingActivity/GetAllByBarn/" + fkBarnId);
        }

        public async Task<RestApiResult<SucklingActivity>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<SucklingActivity>(_client, "SucklingActivity/" + id);
        }

        public async Task<RestApiResult<SucklingActivity>> PostAsync(SucklingActivity model)
        {
            return await HttpClientExtentions.SecuredPostAsync<SucklingActivity>(_client, "SucklingActivity", model);
        }

        public async Task<RestApiResult<SucklingActivity>> PutAsync(SucklingActivity model)
        {
            return await HttpClientExtentions.SecuredPutAsync<SucklingActivity>(_client, "SucklingActivity", model);
        }

        public async Task<RestApiResult<SucklingActivity>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<SucklingActivity>(_client, "SucklingActivity/" + id);
        }
    }
}
