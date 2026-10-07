using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.SmartSheep;

namespace Web.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISucklingActivityService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<SucklingActivity>>> GetAllAsync();
        Task<RestApiResult<List<SucklingActivity>>> GetAllByBarnAsync(Guid fkBarnId);
        Task<RestApiResult<SucklingActivity>> GetAsync(Guid id);
        Task<RestApiResult<SucklingActivity>> PostAsync(SucklingActivity model);
        Task<RestApiResult<SucklingActivity>> PutAsync(SucklingActivity model);
        Task<RestApiResult<SucklingActivity>> DeleteAsync(Guid id);
    }
}
