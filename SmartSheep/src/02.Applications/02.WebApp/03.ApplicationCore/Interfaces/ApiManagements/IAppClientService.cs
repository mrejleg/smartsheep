using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.ApiManagements;

namespace Web.ApplicationCore.Interfaces.ApiManagements
{
    public interface IAppClientService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<AppClient>>> GetAllAsync();
        Task<RestApiResult<List<AppClient>>> GetAll();
        Task<RestApiResult<AppClient>> GetAsync(Guid id);
        Task<RestApiResult<AppClient>> GetAsync(string clientId);
        Task<RestApiResult<AppClient>> PostAsync(AppClient model);
        Task<RestApiResult<AppClient>> PutAsync(AppClient model);
        Task<RestApiResult<AppClient>> DeleteAsync(Guid id);

        Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync();
    }
}
