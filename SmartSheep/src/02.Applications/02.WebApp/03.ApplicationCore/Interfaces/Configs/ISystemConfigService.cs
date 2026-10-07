using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface ISystemConfigService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<SystemConfig>>> GetAllAsync();
        Task<RestApiResult<SystemConfig>> GetAsync(Guid id);
        Task<RestApiResult<SystemConfig>> PostAsync(SystemConfig model);
        Task<RestApiResult<SystemConfig>> PutAsync(SystemConfig model);
        Task<RestApiResult<SystemConfig>> DeleteAsync(Guid id);
    }
}
