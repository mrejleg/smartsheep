using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface IReminderTemplateService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<ReminderTemplate> GetAll();
        Task<ReminderTemplate> GetAsync(Guid id);
        Task<DataPagedResults<ReminderTemplate>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(ReminderTemplate model);
        Task<Guid?> PutAsync(ReminderTemplate model);
        Task<bool> DeleteAsync(Guid id);
    }
}

