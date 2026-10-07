using System.Data;

namespace Project.Base.Models.Dappers
{
    public class DapperTransactionResult
    {
        public IDbConnection Db { get; set; }
        public IDbTransaction Transaction { get; set; }
    }
}
