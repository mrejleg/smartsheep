using Api.Domain.Entities.MasterDatas;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.MasterDatas
{
    public interface IFarmService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<Farm> GetAll();
        Task<Farm> GetAsync(Guid id);
        Task<DataPagedResults<Farm>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(Farm model);
        Task<Guid?> PutAsync(Farm model);
        Task<bool> DeleteAsync(Guid id);
    }
}

