using System.Linq.Expressions;

namespace Project.Core.Interfaces.ExpressionQueries
{
    public interface IWhereClauseQuery<T>
    {
        Expression<Func<T, bool>> WhereCriteria { get; }
    }
}
