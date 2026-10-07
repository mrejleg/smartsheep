using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface IAppMenuService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<AppMenu> GetAll();
        IQueryable<AppMenu> GetAllByParent(Guid fkParentId);
        Task<AppMenu> GetAsync(Guid id);
        Task<DataPagedResults<AppMenu>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(AppMenu model);
        Task<Guid?> PutAsync(AppMenu model);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxParentAsync();
    }
}
