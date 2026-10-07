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
    public class LivestockService : ILivestockService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public LivestockService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<Livestock>(_client, "Livestock/DxGrid", param);
        }

        public async Task<RestApiResult<List<Livestock>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<Livestock>>(_client, "Livestock/All");
        }

        public async Task<RestApiResult<Livestock>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<Livestock>(_client, "Livestock/" + id);
        }

        public async Task<RestApiResult<Livestock>> PostAsync(Livestock model)
        {
            return await HttpClientExtentions.SecuredPostAsync<Livestock>(_client, "Livestock", model);
        }

        public async Task<RestApiResult<Livestock>> PutAsync(Livestock model)
        {
            return await HttpClientExtentions.SecuredPutAsync<Livestock>(_client, "Livestock", model);
        }

        public async Task<RestApiResult<Livestock>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<Livestock>(_client, "Livestock/" + id);
        }
    }
}
