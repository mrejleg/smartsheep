using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.RestApis;
using Web.Domain.Models.SmartSheep;

namespace Web.ApplicationCore.Interfaces.SmartSheep
{
    public interface IReminderLogService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        Task<RestApiResult<List<ReminderLog>>> GetAllAsync();
        Task<RestApiResult<List<ReminderLog>>> GetAllByReminderTemplateAsync(Guid fkReminderTemplateId);
        Task<RestApiResult<List<ReminderLog>>> GetAllBySucklingStatisticAsync(Guid fkSucklingStatisticId);
        Task<RestApiResult<ReminderLog>> GetAsync(Guid id);
        Task<RestApiResult<ReminderLog>> PostAsync(ReminderLog model);
        Task<RestApiResult<ReminderLog>> PutAsync(ReminderLog model);
        Task<RestApiResult<ReminderLog>> DeleteAsync(Guid id);
    }
}
