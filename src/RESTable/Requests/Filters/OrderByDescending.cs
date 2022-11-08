using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Filters;

/// <inheritdoc />
/// <summary>
///     Orders entities in descending order
/// </summary>
public class OrderByDescending : OrderBy
{
    public OrderByDescending(IEntityResource resource, Term term) : base(resource, term) { }

    /// <inheritdoc />
    public override IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities)
    {
        if (Skip) return entities;
        return entities.OrderByDescendingAwait(Selector);
    }

    internal override OrderBy GetCopy()
    {
        return new OrderByDescending(Resource, Term);
    }
}
