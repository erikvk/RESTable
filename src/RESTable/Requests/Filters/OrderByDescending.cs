using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Filters
{
    /// <inheritdoc />
    /// <summary>
    /// Orders entities in descending order
    /// </summary>
    public class OrderByDescending : OrderBy
    {
        /// <inheritdoc />
        public OrderByDescending(IEntityResource resource, string key, ICollection<string> dynamicMembers) : base(resource, key, dynamicMembers) { }

        private OrderByDescending(IEntityResource resource, Term term) : base(resource, term) { }

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

            return entities.OrderByDescending(selector);
        }

        internal override OrderBy GetCopy() => new OrderByDescending(Resource, Term);
    }
}