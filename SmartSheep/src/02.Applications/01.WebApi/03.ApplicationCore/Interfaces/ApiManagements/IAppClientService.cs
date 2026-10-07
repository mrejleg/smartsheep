using Api.Domain.ApiManagements;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.ApiManagements
{
    public interface IAppClientService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<AppClient> GetAll();
        Task<IReadOnlyList<AppClient>> GetAllAsync();
        Task<AppClient> GetAsync(Guid id);
        Task<AppClient> GetAsync(string clientId);
        Task<DataPagedResults<AppClient>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(AppClient model);
        Task<Guid?> PutAsync(AppClient model);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<DxDropDownBoxBinding>> BindingDxDropDownBoxAsync();
    }
}
