using Api.Domain.Entities.Configs;
using DevExtreme.AspNet.Data.ResponseModel;
using Project.Base.Models;
using Project.Base.Models.DevExpress.DxGrids;
using Project.Base.Models.JQueries.Datatables;

namespace Api.ApplicationCore.Interfaces.Configs
{
    public interface INewsService
    {
        Task<LoadResult> DxGridAsync(DataSourceLoadOptions param);
        IQueryable<News> GetAll();
        Task<News> GetAsync(Guid id);
        Task<DataPagedResults<News>> GetAsync(DataParameter param);
        Task<Guid?> PostAsync(News model);
        Task<Guid?> PutAsync(News model);
        Task<bool> DeleteAsync(Guid id);
    }
}
