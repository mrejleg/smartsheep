using Api.Domain.Entities.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.SmartSheep
{
    public interface IJobExecutionLogService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<JobExecutionLog> GetAll();
        Task<JobExecutionLog> GetAsync(Guid id);
        Task<DataPagedResults<JobExecutionLog>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(JobExecutionLog model);
        Task<Guid?> PutAsync(JobExecutionLog model);
        Task<bool> DeleteAsync(Guid id);
    }
}

