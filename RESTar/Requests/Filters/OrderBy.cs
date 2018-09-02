using System.Collections.Generic;
using System.Linq;
using RESTar.Meta;

namespace RESTar.Requests.Filters
{
    /// <summary>
    /// Orders the entities in an IEnumerable based on the values for some property
    /// </summary>
    public class OrderBy : IFilter
    {
        internal Term Term { get; }
        internal string Key => Term.Key;
        internal bool Descending { get; }
        internal bool Ascending => !Descending;
        internal IEntityResource Resource { get; }
        internal bool Skip { get; set; }

        internal OrderBy(IEntityResource resource) => Resource = resource;

        internal OrderBy(IEntityResource resource, bool descending, string key, ICollection<string> dynamicMembers)
        {
            Resource = resource;
            Descending = descending;
            Term = resource.MakeOutputTerm(key, dynamicMembers);
        }

        /// <summary>
        /// Applies the order by operation on an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
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

            return Ascending ? entities.OrderBy(selector) : entities.OrderByDescending(selector);
        }
    }
}