using Api.Domain.Entities.MasterDatas;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.MasterDatas
{
    public interface ILivestockService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<Livestock> GetAll();
        Task<Livestock> GetAsync(Guid id);
        Task<DataPagedResults<Livestock>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(Livestock model);
        Task<Guid?> PutAsync(Livestock model);
        Task<bool> DeleteAsync(Guid id);
    }
}

