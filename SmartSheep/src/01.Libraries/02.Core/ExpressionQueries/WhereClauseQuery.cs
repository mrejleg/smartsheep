using Project.Core.Interfaces.ExpressionQueries;
using System.Linq.Expressions;

namespace Project.Core.ExpressionQueries
{
    public class WhereClauseQuery<TEntity> : IWhereClauseQuery<TEntity>
    {
        public Expression<Func<TEntity, bool>> WhereCriteria { get; }

        public WhereClauseQuery(Expression<Func<TEntity, bool>> whereCriteria)
        {
            WhereCriteria = whereCriteria;
        }
    }
}
