using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.ApiManagements;

namespace Web.ApplicationCore.Interfaces.ApiManagements
{
    public interface IAppScopeClientService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<AppScopeClient>>> GetAllAsync();
        Task<RestApiResult<List<AppScopeClient>>> GetAllAsync(Guid fkAppClientId);
        Task<RestApiResult<List<AppScopeClient>>> GetAllByAppClientAsync(Guid fkAppClientId);
        Task<RestApiResult<List<AppScopeClient>>> GetAllByScopeClientAsync(Guid fkScopeClientId);
        Task<RestApiResult<AppScopeClient>> GetAsync(Guid id);
        Task<RestApiResult<AppScopeClient>> PostAsync(AppScopeClient model);
        Task<RestApiResult<AppScopeClient>> PutAsync(AppScopeClient model);
        Task<RestApiResult<AppScopeClient>> DeleteAsync(Guid id);
        Task<RestApiResult<object>> ReplaceAsync(Guid fkAppClientId, IReadOnlyCollection<AppScopeClient> models);

    }
}
