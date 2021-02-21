using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Filters
{
    /// <inheritdoc />
    /// <summary>
    /// Orders entities in ascending order
    /// </summary>
    public class OrderByAscending : OrderBy
    {
        /// <inheritdoc />
        public OrderByAscending(IEntityResource resource, string key, ICollection<string> dynamicMembers) : base(resource, key, dynamicMembers) { }

        private OrderByAscending(IEntityResource resource, Term term) : base(resource, term) { }

        /// <inheritdoc />
        public override IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities)
        {
            if (Skip) return entities;

            dynamic selector(T i)
            {
                try
                {
                    return Term.Evaluate(i);
                }
                catch
                {
                    return default;
                }
            }

            return entities.OrderBy(selector);
        }

        internal override OrderBy GetCopy() => new OrderByAscending(Resource, Term);
    }
}