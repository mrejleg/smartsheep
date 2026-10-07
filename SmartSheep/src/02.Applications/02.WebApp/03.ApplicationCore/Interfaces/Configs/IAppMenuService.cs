using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxDropDownBoxs;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Select2s;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface IAppMenuService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<AppMenu>>> GetAllAsync();
        Task<RestApiResult<List<AppMenu>>> GetAllAsync(int id);
        Task<RestApiResult<List<AppMenu>>> GetAllByParentAsync(Guid fkParentId);
        Task<RestApiResult<AppMenu>> GetAsync(Guid id);
        Task<RestApiResult<AppMenu>> PostAsync(AppMenu model);
        Task<RestApiResult<AppMenu>> PutAsync(AppMenu model);
        Task<RestApiResult<AppMenu>> DeleteAsync(Guid id);
        Task<RestApiResult<List<DxDropDownBoxBinding>>> BindingDxDropDownBoxAsync();
        Task<RestApiResult<List<Select2Binding>>> BindingSelect2AccessTypes();
    }
}
