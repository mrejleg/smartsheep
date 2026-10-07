using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.ApiManagements;

namespace Web.ApplicationCore.Interfaces.ApiManagements
{
    public interface IScopeClientService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<ScopeClient>>> GetAllAsync();
        Task<RestApiResult<ScopeClient>> GetAsync(Guid id);
        Task<RestApiResult<ScopeClient>> GetAsync(string key);
        Task<RestApiResult<ScopeClient>> PostAsync(ScopeClient model);
        Task<RestApiResult<ScopeClient>> PutAsync(ScopeClient model);
        Task<RestApiResult<ScopeClient>> DeleteAsync(Guid id);

        Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync();
    }
}
