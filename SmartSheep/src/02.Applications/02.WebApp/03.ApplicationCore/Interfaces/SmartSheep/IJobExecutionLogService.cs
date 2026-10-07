using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.SmartSheep;

namespace Web.ApplicationCore.Interfaces.SmartSheep
{
    public interface IJobExecutionLogService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<JobExecutionLog>>> GetAllAsync();
        Task<RestApiResult<JobExecutionLog>> GetAsync(Guid id);
        Task<RestApiResult<JobExecutionLog>> PostAsync(JobExecutionLog model);
        Task<RestApiResult<JobExecutionLog>> PutAsync(JobExecutionLog model);
        Task<RestApiResult<JobExecutionLog>> DeleteAsync(Guid id);
    }
}
