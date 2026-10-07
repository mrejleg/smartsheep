using Dapper;
using Project.Base.Models.Dappers;
using System.Data;

namespace Project.Core.Interfaces.Repositories.Dappers
{
    public interface IDapperRepository
    {
        string ConnectionString { get; set; }
        string DatabaseProvider { get; set; }
        DapperTransactionResult DbTransaction();
        Task<TEntity> GetAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType);
        Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType);
        Task<List<TEntity>> GetAllListAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType);
        Task<IEnumerable<TEntity>> ExecuteQueryAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType);
        Task<int> ExecuteAsync(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType);
        Task<TEntity> ExecuteScalarAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType);
    }
}
