using Microsoft.EntityFrameworkCore;
using Project.Base.Models;

namespace Project.Core.Interfaces.Repositories
{
    public interface IRepositoryPattern<TEntity, TId> where TEntity : class, new()
    {
        DbSet<TEntity> DataContext();

        IQueryable<TEntity> GetAll();

        Task<IReadOnlyList<TEntity>> GetAllAsync();
        Task<DataPagedResults<TEntity>> GetAllByPagingAsync(int start, int length);

        Task<DataPagedResults<TEntity>> GetByPagingAsync(IQueryable<TEntity> data, int start, int length);

        DataPagedResults<TEntity> GetByPaging(IReadOnlyList<TEntity> data, int start, int length);

        Task<TEntity> GetAsync(TId id);

        Task<int> CountAsync();

        Task<TEntity> AddAsync(TEntity entity);

        Task<List<TEntity>> AddRangeAsync(List<TEntity> entities);

        Task UpdateAsync(TEntity entity);

        Task UpdateRangeAsync(List<TEntity> entities);

        Task DeleteAsync(TEntity entity);

        Task DeleteRangrAsync(IReadOnlyList<TEntity> entity);
    }
}
