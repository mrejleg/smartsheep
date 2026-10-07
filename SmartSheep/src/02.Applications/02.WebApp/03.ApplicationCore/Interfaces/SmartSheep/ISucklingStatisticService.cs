using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.SmartSheep;

namespace Web.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISucklingStatisticService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<SucklingStatistic>>> GetAllAsync();
        Task<RestApiResult<SucklingStatistic>> GetAsync(Guid id);
        Task<RestApiResult<SucklingStatistic>> PostAsync(SucklingStatistic model);
        Task<RestApiResult<SucklingStatistic>> PutAsync(SucklingStatistic model);
        Task<RestApiResult<SucklingStatistic>> DeleteAsync(Guid id);
    }
}
