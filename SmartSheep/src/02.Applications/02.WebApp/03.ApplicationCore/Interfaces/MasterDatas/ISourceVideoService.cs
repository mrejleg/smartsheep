using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.MasterDatas;

namespace Web.ApplicationCore.Interfaces.MasterDatas
{
    public interface ISourceVideoService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<SourceVideo>>> GetAllAsync();
        Task<RestApiResult<List<SourceVideo>>> GetAllByBarnAsync(Guid fkBarnId);
        Task<RestApiResult<SourceVideo>> GetAsync(Guid id);
        Task<RestApiResult<SourceVideo>> PostAsync(SourceVideo model);
        Task<RestApiResult<SourceVideo>> PutAsync(SourceVideo model);
        Task<RestApiResult<SourceVideo>> DeleteAsync(Guid id);
    }
}
