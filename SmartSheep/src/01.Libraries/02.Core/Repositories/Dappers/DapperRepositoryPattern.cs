using Dapper;
using Project.Base.Models.Dappers;
using Project.Core.Interfaces.Repositories.Dappers;
using System.Data;
using System.Data.SqlClient;

namespace Project.Core.Repositories.Dappers
{
    public class DapperRepositoryPattern : IDapperRepositoryPattern
    {
        private string ConnString = string.Empty;
        private string DbProvider = string.Empty;

        public DapperRepositoryPattern()
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

        public async Task<List<TEntity>> GetAllListAsync<TEntity>()
        {
            try
            {
                var tableName = GetTableName<TEntity>();
                if (!string.IsNullOrEmpty(tableName))
                {
                    var sqlQuery = string.Format("SELECT * FROM {0}", tableName);
                    var data = await DapperDb().QueryAsync<TEntity>(sqlQuery, null, commandType: CommandType.Text);

                    return data.AsList();
                }

                return new List<TEntity>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync<TEntity>()
        {
            try
            {
                var tableName = GetTableName<TEntity>();
                if (!string.IsNullOrEmpty(tableName))
                {
                    var sqlQuery = string.Format("SELECT * FROM {0}", tableName);
                    return await DapperDb().QueryAsync<TEntity>(sqlQuery, null, commandType: CommandType.Text);
                }

                return new List<TEntity>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        public async Task<IEnumerable<object>> GetAllAsync<TEntity>(string tableName)
        {
            try
            {
                if (!string.IsNullOrEmpty(tableName))
                {
                    var sqlQuery = string.Format("SELECT * FROM {0}", tableName);
                    var data = await DapperDb().QueryAsync<object>(sqlQuery, null, commandType: CommandType.Text);

                    return await Task.FromResult(data);
                }

                return new List<object>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        private static string GetTableName<TEntity>()
        {
            try
            {
                var result = typeof(TEntity).GetCustomAttributesData();

                var customAttributeData = result.FirstOrDefault(x => x.AttributeType.Name == "TableAttribute");
                if (customAttributeData != null)
                {
                    return customAttributeData.ConstructorArguments[0].Value.ToString();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }
    }
}
