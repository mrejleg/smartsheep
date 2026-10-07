using Microsoft.EntityFrameworkCore;
using Project.Base.Models;
using Project.Core.Interfaces.Repositories;

namespace Project.Core.Repositories
{
    public class RepositoryPattern<TEntity, TId> : IRepositoryPattern<TEntity, TId> where TEntity : class, new()
    {
        protected DbContext _dbContext;

        public RepositoryPattern(DbContext dbContext)
        {
            _dbContext = dbContext;
            _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        // Skip/Take tanpa OrderBy membuat SQL Server memakai "ORDER BY (SELECT 1)": urutan
        // baris tidak dijamin sehingga satu baris bisa muncul di dua halaman atau hilang.
        // Kalau query belum diurutkan, urutkan berdasarkan primary key.
        private IQueryable<TEntity> EnsureOrdered(IQueryable<TEntity> data)
        {
            // Catatan: cek "is IOrderedQueryable" TIDAK bisa dipakai — Set<T>().AsNoTracking()
            // sudah bertipe IOrderedQueryable walau belum ada OrderBy. Jadi deteksi ordering
            // sebenarnya lewat pemindaian expression tree.
            if (HasOrderBy(data.Expression))
            {
                return data;
            }

            var keyName = _dbContext.Model.FindEntityType(typeof(TEntity))?.FindPrimaryKey()?.Properties
                .Select(p => p.Name).FirstOrDefault();

            return string.IsNullOrEmpty(keyName)
                ? data
                : data.OrderBy(e => EF.Property<object>(e, keyName));
        }

        private static bool HasOrderBy(System.Linq.Expressions.Expression expression)
        {
            var visitor = new OrderByFinder();
            visitor.Visit(expression);
            return visitor.Found;
        }

        private sealed class OrderByFinder : System.Linq.Expressions.ExpressionVisitor
        {
            private static readonly HashSet<string> OrderMethods = new()
            {
                "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"
            };

            public bool Found { get; private set; }

            protected override System.Linq.Expressions.Expression VisitMethodCall(System.Linq.Expressions.MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(Queryable) && OrderMethods.Contains(node.Method.Name))
                {
                    Found = true;
                }

                return base.VisitMethodCall(node);
            }
        }

        public async Task<DataPagedResults<TEntity>> GetAllByPagingAsync(int start, int length)
        {
            var data = GetAll();
            var size = await data.AsNoTracking().CountAsync();

            TEntity[] items;
            if (start > 0 && length > 0)
            {
                items = await EnsureOrdered(data)
                .Skip((start - 1) * length)
                .Take(length)
                .ToArrayAsync();
            }
            else
            {
                items = await data
                .ToArrayAsync();
            }

            return new DataPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public async Task<DataPagedResults<TEntity>> GetByPagingAsync(IQueryable<TEntity> data, int start, int length)
        {
            var size = await data.AsNoTracking().CountAsync();

            TEntity[] items;
            if (start > 0 && length > 0)
            {
                items = await EnsureOrdered(data)
                .Skip((start - 1) * length)
                .Take(length)
                .ToArrayAsync();
            }
            else
            {
                items = await data
                .ToArrayAsync();
            }

            return new DataPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public DataPagedResults<TEntity> GetByPaging(IReadOnlyList<TEntity> data, int start, int length)
        {
            var size = data.Count;

            TEntity[] items;
            if (start > 0 && length > 0)
            {
                items = data
                .Skip((start - 1) * length)
                .Take(length)
                .ToArray();
            }
            else
            {
                items = data
                .ToArray();
            }

            return new DataPagedResults<TEntity>
            {
                Items = items,
                TotalSize = size
            };
        }

        public IQueryable<TEntity> GetAll()
        {
            return _dbContext.Set<TEntity>().AsNoTracking();
        }

        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
        {
            return await _dbContext.Set<TEntity>().AsNoTracking().ToListAsync();
        }

        public async Task<TEntity> GetAsync(TId id)
        {
            var data = await _dbContext.Set<TEntity>().FindAsync(id);
            if (data != null)
            {
                _dbContext.Entry(data).State = EntityState.Detached;
            }

            return data!;
        }

        public async Task<int> CountAsync()
        {
            return await _dbContext.Set<TEntity>().AsNoTracking().CountAsync();
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return entity;
        }

        public async Task<List<TEntity>> AddRangeAsync(List<TEntity> entities)
        {
            await _dbContext.Set<TEntity>().AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();

            return entities;
        }

        public async Task UpdateAsync(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateRangeAsync(List<TEntity> entities)
        {
            _dbContext.UpdateRange(entities);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(TEntity entity)
        {
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteRangrAsync(IReadOnlyList<TEntity> entity)
        {
            _dbContext.Set<TEntity>().RemoveRange(entity.ToArray());
            await _dbContext.SaveChangesAsync();
        }

        public DbSet<TEntity> DataContext()
        {
            return _dbContext.Set<TEntity>();
        }
    }
}
