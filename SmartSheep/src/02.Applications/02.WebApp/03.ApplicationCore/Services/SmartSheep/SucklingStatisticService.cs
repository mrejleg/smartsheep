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
    public class SucklingStatisticService : ISucklingStatisticService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public SucklingStatisticService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<SucklingStatistic>(_client, "SucklingStatistic/DxGrid", param);
        }

        public async Task<RestApiResult<List<SucklingStatistic>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SucklingStatistic>>(_client, "SucklingStatistic/All");
        }

        public async Task<RestApiResult<SucklingStatistic>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<SucklingStatistic>(_client, "SucklingStatistic/" + id);
        }

        public async Task<RestApiResult<SucklingStatistic>> PostAsync(SucklingStatistic model)
        {
            return await HttpClientExtentions.SecuredPostAsync<SucklingStatistic>(_client, "SucklingStatistic", model);
        }

        public async Task<RestApiResult<SucklingStatistic>> PutAsync(SucklingStatistic model)
        {
            return await HttpClientExtentions.SecuredPutAsync<SucklingStatistic>(_client, "SucklingStatistic", model);
        }

        public async Task<RestApiResult<SucklingStatistic>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<SucklingStatistic>(_client, "SucklingStatistic/" + id);
        }
    }
}
