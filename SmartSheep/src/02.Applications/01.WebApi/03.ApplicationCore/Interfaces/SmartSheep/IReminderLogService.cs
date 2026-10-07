using Api.Domain.Entities.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.SmartSheep
{
    public interface IReminderLogService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<ReminderLog> GetAll();
        IQueryable<ReminderLog> GetAllByReminderTemplate(Guid fkReminderTemplateId);
        IQueryable<ReminderLog> GetAllBySucklingStatistic(Guid fkSucklingStatisticId);
        Task<ReminderLog> GetAsync(Guid id);
        Task<DataPagedResults<ReminderLog>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(ReminderLog model);
        Task<Guid?> PutAsync(ReminderLog model);
        Task<bool> DeleteAsync(Guid id);
    }
}
