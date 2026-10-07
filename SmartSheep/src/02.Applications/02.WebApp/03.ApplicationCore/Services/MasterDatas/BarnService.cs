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
    public class BarnService : IBarnService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public BarnService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<Barn>(_client, "Barn/DxGrid", param);
        }

        public async Task<RestApiResult<List<Barn>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<Barn>>(_client, "Barn/All");
        }

        public async Task<RestApiResult<List<Barn>>> GetAllByFarmAsync(Guid fkFarmId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<Barn>>(_client, "Barn/GetAllByFarm/" + fkFarmId);
        }

        public async Task<RestApiResult<List<Barn>>> GetAllByLivestockAsync(Guid fkLivestockId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<Barn>>(_client, "Barn/GetAllByLivestock/" + fkLivestockId);
        }

        public async Task<RestApiResult<Barn>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<Barn>(_client, "Barn/" + id);
        }

        public async Task<RestApiResult<Barn>> PostAsync(Barn model)
        {
            return await HttpClientExtentions.SecuredPostAsync<Barn>(_client, "Barn", model);
        }

        public async Task<RestApiResult<Barn>> PutAsync(Barn model)
        {
            return await HttpClientExtentions.SecuredPutAsync<Barn>(_client, "Barn", model);
        }

        public async Task<RestApiResult<Barn>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<Barn>(_client, "Barn/" + id);
        }
    }
}
