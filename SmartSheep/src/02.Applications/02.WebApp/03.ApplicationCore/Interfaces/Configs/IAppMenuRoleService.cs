using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface IAppMenuRoleService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<AppMenuRole>>> GetAllAsync();
        Task<RestApiResult<List<AppMenuRole>>> GetAllAsync(Guid id);
        Task<RestApiResult<List<AppMenuRole>>> GetAllByAppRoleAsync(Guid fkAppRoleId);
        Task<RestApiResult<List<AppMenuRole>>> GetAllByAppMenuAsync(Guid fkAppMenuId);
        Task<RestApiResult<AppMenuRole>> GetAsync(Guid id);
        Task<RestApiResult<AppMenuRole>> PostAsync(AppMenuRole model);
        Task<RestApiResult<AppMenuRole>> PutAsync(AppMenuRole model);
        Task<RestApiResult<AppMenuRole>> DeleteAsync(Guid id);
        Task<RestApiResult<object>> ReplaceAsync(Guid fkAppRoleId, IReadOnlyCollection<AppMenuRole> models);
    }
}
