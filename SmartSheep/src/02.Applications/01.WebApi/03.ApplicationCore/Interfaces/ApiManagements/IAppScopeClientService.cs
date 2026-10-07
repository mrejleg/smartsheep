using Api.Domain.ApiManagements;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.ApiManagements
{
    public interface IAppScopeClientService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<AppScopeClient> GetAll();
        IQueryable<AppScopeClient> GetAll(Guid fkAppClientId);
        IQueryable<AppScopeClient> GetAllByAppClient(Guid fkAppClientId);
        IQueryable<AppScopeClient> GetAllByScopeClient(Guid fkScopeClientId);
        Task<AppScopeClient> GetAsync(Guid id);
        Task<DataPagedResults<AppScopeClient>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(AppScopeClient model);
        Task<Guid?> PutAsync(AppScopeClient model);
        Task<bool> DeleteAsync(Guid id);
        Task<int?> ReplaceAsync(Guid fkAppClientId, IReadOnlyCollection<AppScopeClient> models);
    }
}
