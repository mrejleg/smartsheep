using Api.Domain.Entities.MasterDatas;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.MasterDatas
{
    public interface IBarnService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<Barn> GetAll();
        IQueryable<Barn> GetAllByFarm(Guid fkFarmId);
        IQueryable<Barn> GetAllByLivestock(Guid fkLivestockId);
        Task<Barn> GetAsync(Guid id);
        Task<DataPagedResults<Barn>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(Barn model);
        Task<Guid?> PutAsync(Barn model);
        Task<bool> DeleteAsync(Guid id);
    }
}
