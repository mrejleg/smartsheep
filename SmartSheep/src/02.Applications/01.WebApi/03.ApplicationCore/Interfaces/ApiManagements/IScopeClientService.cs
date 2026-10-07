using Api.Domain.ApiManagements;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.ApiManagements
{
    public interface IScopeClientService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<ScopeClient> GetAll();
        Task<ScopeClient> GetAsync(Guid id);
        Task<ScopeClient> GetAsync(string key);
        Task<DataPagedResults<ScopeClient>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(ScopeClient model);
        Task<Guid?> PutAsync(ScopeClient model);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync();
    }
}
