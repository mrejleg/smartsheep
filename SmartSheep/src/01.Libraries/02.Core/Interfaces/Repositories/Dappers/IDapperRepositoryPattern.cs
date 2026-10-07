using Project.Base.Models.Dappers;

namespace Project.Core.Interfaces.Repositories.Dappers
{
    public interface IDapperRepositoryPattern
    {
        string ConnectionString { get; set; }
        string DatabaseProvider { get; set; }
        DapperTransactionResult DbTransaction();
        Task<IEnumerable<TEntity>> GetAllAsync<TEntity>();
        Task<List<TEntity>> GetAllListAsync<TEntity>();
        Task<IEnumerable<object>> GetAllAsync<TEntity>(string tableName);
    }
}
