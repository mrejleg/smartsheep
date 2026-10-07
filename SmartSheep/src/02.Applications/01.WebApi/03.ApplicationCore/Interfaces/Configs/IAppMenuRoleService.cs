using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface IAppMenuRoleService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<AppMenuRole> GetAll();
        IQueryable<AppMenuRole> GetAllByAppRole(Guid fkAppRoleId);
        IQueryable<AppMenuRole> GetAllByAppMenu(Guid fkAppMenuId);
        Task<AppMenuRole> GetAsync(Guid id);
        Task<DataPagedResults<AppMenuRole>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(AppMenuRole model);
        Task<Guid?> PutAsync(AppMenuRole model);
        Task<bool> DeleteAsync(Guid id);
        Task<int?> ReplaceAsync(Guid fkAppRoleId, IReadOnlyCollection<AppMenuRole> models);
    }
}
