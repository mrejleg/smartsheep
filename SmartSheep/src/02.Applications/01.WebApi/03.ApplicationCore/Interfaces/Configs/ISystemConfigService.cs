using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface ISystemConfigService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<SystemConfig> GetAll();
        Task<SystemConfig> GetAsync(Guid id);
        Task<DataPagedResults<SystemConfig>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(SystemConfig model);
        Task<Guid?> PutAsync(SystemConfig model);
        Task<bool> DeleteAsync(Guid id);
    }
}

