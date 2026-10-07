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
    public class JobExecutionLogService : IJobExecutionLogService
    {
        private readonly HttpClient _client;
        private readonly AppSetting _appSettings;

        public JobExecutionLogService(AppSetting appSettings)
        {
            _appSettings = appSettings;
            _client = HttpClientFactoryExtentions.ClientApiBearear(MapperAppSettings.ParamAppSetting(_appSettings));
        }

        public async Task<LoadResult> DxGridAsync(DataSourceLoadOptions param)
        {
            return await HttpClientExtentions.SecuredDxGridAsync<JobExecutionLog>(_client, "JobExecutionLog/DxGrid", param);
        }

        public async Task<RestApiResult<List<JobExecutionLog>>> GetAllAsync()
        {
            return await HttpClientExtentions.SecuredGetAsync<List<JobExecutionLog>>(_client, "JobExecutionLog/All");
        }

        public async Task<RestApiResult<JobExecutionLog>> GetAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredGetAsync<JobExecutionLog>(_client, "JobExecutionLog/" + id);
        }

        public async Task<RestApiResult<JobExecutionLog>> PostAsync(JobExecutionLog model)
        {
            return await HttpClientExtentions.SecuredPostAsync<JobExecutionLog>(_client, "JobExecutionLog", model);
        }

        public async Task<RestApiResult<JobExecutionLog>> PutAsync(JobExecutionLog model)
        {
            return await HttpClientExtentions.SecuredPutAsync<JobExecutionLog>(_client, "JobExecutionLog", model);
        }

        public async Task<RestApiResult<JobExecutionLog>> DeleteAsync(Guid id)
        {
            return await HttpClientExtentions.SecuredDeleteAsync<JobExecutionLog>(_client, "JobExecutionLog/" + id);
        }
    }
}
