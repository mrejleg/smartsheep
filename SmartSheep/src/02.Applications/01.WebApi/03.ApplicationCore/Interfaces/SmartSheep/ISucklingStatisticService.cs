using Api.Domain.Entities.SmartSheep;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.SmartSheep
{
    public interface ISucklingStatisticService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<SucklingStatistic> GetAll();
        Task<SucklingStatistic> GetAsync(Guid id);
        Task<DataPagedResults<SucklingStatistic>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(SucklingStatistic model);
        Task<Guid?> PutAsync(SucklingStatistic model);
        Task<bool> DeleteAsync(Guid id);
        Task RecalculateAsync(Guid fkBarnId, DateTime from, DateTime processedUntil);
    }
}

