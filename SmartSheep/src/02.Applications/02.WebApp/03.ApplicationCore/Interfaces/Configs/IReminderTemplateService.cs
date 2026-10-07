using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.Firebases;
using Project.Base.Models.RestApis;
using Web.Domain.Models.Configs;

namespace Web.ApplicationCore.Interfaces.Configs
{
    public interface IReminderTemplateService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<ReminderTemplate>>> GetAllAsync();
        Task<RestApiResult<ReminderTemplate>> GetAsync(Guid id);
        Task<RestApiResult<ReminderTemplate>> PostAsync(ReminderTemplate model);
        Task<RestApiResult<ReminderTemplate>> PutAsync(ReminderTemplate model);
        Task<RestApiResult<ReminderTestResultDto>> TestAsync(Guid id);
        Task<RestApiResult<ReminderTemplate>> DeleteAsync(Guid id);
    }
}
