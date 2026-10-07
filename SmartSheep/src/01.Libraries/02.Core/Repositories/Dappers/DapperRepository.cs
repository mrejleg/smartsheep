using Dapper;
using Project.Base.Models.Dappers;
using Project.Core.Interfaces.Repositories.Dappers;
using System.Data;
using System.Data.SqlClient;

namespace Project.Core.Repositories.Dappers
{
    public class DapperRepository : IDapperRepository
    {
        private string ConnString = string.Empty;
        private string DbProvider = string.Empty;

        public DapperRepository()
        {
        }

        public string ConnectionString
        {
            get
            {
                return ConnString;
            }
            set
            {
                ConnString = value;
            }
        }

        public string DatabaseProvider
        {
            get
            {
                return DbProvider;
            }
            set
            {
                DbProvider = value;
            }
        }

        public DapperTransactionResult DbTransaction()
        {
            var db = DapperDb();
            db.Open();

            var result = new DapperTransactionResult
            {
                Db = db,
                Transaction = db.BeginTransaction()
            };

            return result;
        }

        private IDbConnection DapperDb()
        {
            if (!string.IsNullOrEmpty(DbProvider))
            {
                if (DbProvider.ToLower() == "sqlserver")
                {
                    return new SqlConnection(ConnString);
                }
            }

            return null;
        }

        public async Task<TEntity> GetAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType)
        {
            try
            {
                return await DapperDb().QueryFirstOrDefaultAsync<TEntity>(sqlQuery, dynamicParams, commandType: commandType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType)
        {
            try
            {
                return await DapperDb().QueryAsync<TEntity>(sqlQuery, dynamicParams, commandType: commandType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<List<TEntity>> GetAllListAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType)
        {
            try
            {
                var data = await DapperDb().QueryAsync<TEntity>(sqlQuery, dynamicParams, commandType: commandType);
                return data.AsList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<IEnumerable<TEntity>> ExecuteQueryAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType)
        {
            try
            {
                return await DapperDb().QueryAsync<TEntity>(sqlQuery, dynamicParams, commandType: commandType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<int> ExecuteAsync(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType)
        {
            try
            {
                return await DapperDb().ExecuteAsync(sqlQuery, dynamicParams, commandType: commandType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<TEntity> ExecuteScalarAsync<TEntity>(string sqlQuery, DynamicParameters dynamicParams, CommandType commandType)
        {
            try
            {
                return await DapperDb().ExecuteScalarAsync<TEntity>(sqlQuery, dynamicParams, commandType: commandType);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }
    }
}
