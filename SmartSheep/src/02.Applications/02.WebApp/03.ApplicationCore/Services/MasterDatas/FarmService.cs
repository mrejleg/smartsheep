using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Authorization;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Project.Core.HttpClients;
using Web.ApplicationCore.Interfaces.MasterDatas;
using Web.Domain.Models;
using Web.Domain.Models.MasterDatas;
using Web.Infrastructure;

namespace Web.ApplicationCore.Services.MasterDatas
{
    [Authorize(Policy = "Bearer")]
    public class FarmService : IFarmService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public FarmService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<Farm>(_client, "Farm/DxGrid", param);
        }

        public async Task<RestApiResult<List<Farm>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<Farm>>(_client, "Farm/All");
        }

        public async Task<RestApiResult<Farm>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<Farm>(_client, "Farm/" + id);
        }

        public async Task<RestApiResult<Farm>> PostAsync(Farm model)
        {
            return await HttpClientExtentions.SecuredPostAsync<Farm>(_client, "Farm", model);
        }

        public async Task<RestApiResult<Farm>> PutAsync(Farm model)
        {
            return await HttpClientExtentions.SecuredPutAsync<Farm>(_client, "Farm", model);
        }

        public async Task<RestApiResult<Farm>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<Farm>(_client, "Farm/" + id);
        }
    }
}
