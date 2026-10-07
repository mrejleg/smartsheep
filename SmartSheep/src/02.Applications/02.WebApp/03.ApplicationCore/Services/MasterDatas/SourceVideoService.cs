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
    public class SourceVideoService : ISourceVideoService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public SourceVideoService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<SourceVideo>(_client, "SourceVideo/DxGrid", param);
        }

        public async Task<RestApiResult<List<SourceVideo>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SourceVideo>>(_client, "SourceVideo/All");
        }

        public async Task<RestApiResult<List<SourceVideo>>> GetAllByBarnAsync(Guid fkBarnId)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SourceVideo>>(_client, "SourceVideo/GetAllByBarn/" + fkBarnId);
        }

        public async Task<RestApiResult<SourceVideo>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<SourceVideo>(_client, "SourceVideo/" + id);
        }

        public async Task<RestApiResult<SourceVideo>> PostAsync(SourceVideo model)
        {
            return await HttpClientExtentions.SecuredPostAsync<SourceVideo>(_client, "SourceVideo", model);
        }

        public async Task<RestApiResult<SourceVideo>> PutAsync(SourceVideo model)
        {
            return await HttpClientExtentions.SecuredPutAsync<SourceVideo>(_client, "SourceVideo", model);
        }

        public async Task<RestApiResult<SourceVideo>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<SourceVideo>(_client, "SourceVideo/" + id);
        }
    }
}
