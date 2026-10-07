using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface IAppRoleService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<AppRole> GetAll();
        Task<AppRole> GetAsync(Guid id);
        Task<AppRole> GetByNameAsync(string name);
        Task<DataPagedResults<AppRole>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(AppRole model);
        Task<Guid?> PutAsync(AppRole model);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync();
    }
}
