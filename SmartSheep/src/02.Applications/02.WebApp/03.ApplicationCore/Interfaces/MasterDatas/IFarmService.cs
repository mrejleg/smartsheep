using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Interfaces.MasterDatas
{
    public interface IFarmService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<Farm>>> GetAllAsync();
        Task<RestApiResult<Farm>> GetAsync(Guid id);
        Task<RestApiResult<Farm>> PostAsync(Farm model);
        Task<RestApiResult<Farm>> PutAsync(Farm model);
        Task<RestApiResult<Farm>> DeleteAsync(Guid id);
    }
}
