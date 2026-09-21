

using System.Linq.Expressions;
using Ledger.Domain.Abstractions;

namespace Ledger.Application.Abstractions;

public interface ISpecification<TEntity> where TEntity : Entity
{
    Expression<Func<TEntity, bool>> Criteria { get; }
    List<string> Includes { get; }
}

public abstract class Specification<TEntity>(
    Expression<Func<TEntity, bool>> criteria)
    : ISpecification<TEntity> where TEntity : Entity
{
    public Expression<Func<TEntity, bool>> Criteria { get; private set; } = criteria;
    public List<string> Includes { get; } = [];
    protected void AddIncludes(params string[] includes) => Includes.AddRange(includes);
}
