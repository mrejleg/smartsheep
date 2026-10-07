using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface IAppRoleService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<AppRole>>> GetAllAsync();
        Task<RestApiResult<List<AppRole>>> GetAllAsync(int id);
        Task<RestApiResult<AppRole>> GetAsync(Guid id);
        Task<RestApiResult<AppRole>> GetAsync(string name);
        Task<RestApiResult<AppRole>> PostAsync(AppRole model);
        Task<RestApiResult<AppRole>> PutAsync(AppRole model);
        Task<RestApiResult<AppRole>> DeleteAsync(Guid id);

        Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync();
    }
}
