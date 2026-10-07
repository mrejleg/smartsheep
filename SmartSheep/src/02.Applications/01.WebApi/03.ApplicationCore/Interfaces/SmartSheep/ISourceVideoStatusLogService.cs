using Api.Domain.Entities.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISourceVideoStatusLogService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<SourceVideoStatusLog> GetAll();
        IQueryable<SourceVideoStatusLog> GetAllBySourceVideo(Guid fkIdSourceVideo);
        Task<SourceVideoStatusLog> GetAsync(Guid id);
        Task<DataPagedResults<SourceVideoStatusLog>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(SourceVideoStatusLog model);
        Task<Guid?> PutAsync(SourceVideoStatusLog model);
        Task<bool> DeleteAsync(Guid id);
    }
}
