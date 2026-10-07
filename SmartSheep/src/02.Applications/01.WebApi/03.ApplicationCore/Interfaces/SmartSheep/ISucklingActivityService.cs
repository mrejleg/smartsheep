using Api.Domain.Entities.SmartSheep;
using Api.Domain.Models.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISucklingActivityService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<SucklingActivity> GetAll();
        IQueryable<SucklingActivity> GetAllByBarn(Guid fkBarnId);
        Task<SucklingActivity> GetAsync(Guid id);
        Task<DataPagedResults<SucklingActivity>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(SucklingActivity model);
        Task<Guid?> PutAsync(SucklingActivity model);
        Task<bool> DeleteAsync(Guid id);
        Task<List<SucklingSyncSourceDto>> GetSyncSourcesAsync();
        Task<SucklingSyncSourceDto> GetSyncSourceAsync(string code);
        Task<int?> SyncAsync(SucklingSyncParam param);
    }
}
