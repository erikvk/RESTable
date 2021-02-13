using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Filters
{
    /// <summary>
    /// Orders the entities in an IEnumerable based on the values for some property
    /// </summary>
    public abstract class OrderBy : IFilter
    {
        internal Term Term { get; }
        internal string Key => Term.Key;
        internal IEntityResource Resource { get; }
        internal bool Skip { get; set; }

        internal OrderBy(IEntityResource resource, string key, ICollection<string> dynamicMembers)
        {
            Resource = resource;
            Term = resource.MakeOutputTerm(key, dynamicMembers);
        }

        internal OrderBy(IEntityResource resource, Term term)
        {
            Resource = resource;
            Term = term;
        }

        /// <summary>
        /// Applies the order by operation on an IEnumerable of entities
        /// </summary>
        public abstract IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : class;

        internal abstract OrderBy GetCopy();
    }

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