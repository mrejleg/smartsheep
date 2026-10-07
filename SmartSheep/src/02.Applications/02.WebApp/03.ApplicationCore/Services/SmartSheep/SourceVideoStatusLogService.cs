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
    public class SourceVideoStatusLogService : ISourceVideoStatusLogService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public SourceVideoStatusLogService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<SourceVideoStatusLog>(_client, "SourceVideoStatusLog/DxGrid", param);
        }

        public async Task<RestApiResult<List<SourceVideoStatusLog>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SourceVideoStatusLog>>(_client, "SourceVideoStatusLog/All");
        }

        public async Task<RestApiResult<List<SourceVideoStatusLog>>> GetAllBySourceVideoAsync(Guid fkIdSourceVideo)
        {
            return await HttpClientExtentions.SecuredGetAsync<List<SourceVideoStatusLog>>(_client, "SourceVideoStatusLog/GetAllBySourceVideo/" + fkIdSourceVideo);
        }

        public async Task<RestApiResult<SourceVideoStatusLog>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<SourceVideoStatusLog>(_client, "SourceVideoStatusLog/" + id);
        }

        public async Task<RestApiResult<SourceVideoStatusLog>> PostAsync(SourceVideoStatusLog model)
        {
            return await HttpClientExtentions.SecuredPostAsync<SourceVideoStatusLog>(_client, "SourceVideoStatusLog", model);
        }

        public async Task<RestApiResult<SourceVideoStatusLog>> PutAsync(SourceVideoStatusLog model)
        {
            return await HttpClientExtentions.SecuredPutAsync<SourceVideoStatusLog>(_client, "SourceVideoStatusLog", model);
        }

        public async Task<RestApiResult<SourceVideoStatusLog>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<SourceVideoStatusLog>(_client, "SourceVideoStatusLog/" + id);
        }
    }
}
