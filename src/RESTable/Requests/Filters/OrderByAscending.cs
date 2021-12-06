using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Filters;

/// <inheritdoc />
/// <summary>
///     Orders entities in ascending order
/// </summary>
public class OrderByAscending : OrderBy
{
    public OrderByAscending(IEntityResource resource, Term term) : base(resource, term) { }

    /// <inheritdoc />
    public override IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities)
    {
        if (Skip) return entities;
        return entities.OrderByAwait(Selector);
    }

    internal override OrderBy GetCopy()
    {
        return new OrderByAscending(Resource, Term);
    }
}