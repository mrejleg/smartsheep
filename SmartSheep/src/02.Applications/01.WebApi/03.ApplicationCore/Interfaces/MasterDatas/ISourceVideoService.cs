using Api.Domain.Entities.MasterDatas;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.MasterDatas
{
    public interface ISourceVideoService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<SourceVideo> GetAll();
        IQueryable<SourceVideo> GetAllByBarn(Guid fkBarnId);
        Task<SourceVideo> GetAsync(Guid id);
        Task<DataPagedResults<SourceVideo>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(SourceVideo model);
        Task<Guid?> PutAsync(SourceVideo model);
        Task<bool> DeleteAsync(Guid id);
    }
}
