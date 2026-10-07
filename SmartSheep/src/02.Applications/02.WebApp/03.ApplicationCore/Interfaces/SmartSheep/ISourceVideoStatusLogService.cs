using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.SmartSheep;

namespace Web.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISourceVideoStatusLogService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<SourceVideoStatusLog>>> GetAllAsync();
        Task<RestApiResult<List<SourceVideoStatusLog>>> GetAllBySourceVideoAsync(Guid fkIdSourceVideo);
        Task<RestApiResult<SourceVideoStatusLog>> GetAsync(Guid id);
        Task<RestApiResult<SourceVideoStatusLog>> PostAsync(SourceVideoStatusLog model);
        Task<RestApiResult<SourceVideoStatusLog>> PutAsync(SourceVideoStatusLog model);
        Task<RestApiResult<SourceVideoStatusLog>> DeleteAsync(Guid id);
    }
}
