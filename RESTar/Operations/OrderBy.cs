using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.Operations
{
    /// <summary>
    /// Orders the entities in an IEnumerable based on the values for some property
    /// </summary>
    public class OrderBy : IFilter
    {
        internal string Key => Term.Key;
        internal bool Descending { get; }
        internal bool Ascending => !Descending;
        internal IResource Resource { get; }
        internal readonly Term Term;
        internal bool IsSqlQueryable = true;

        internal string SQL => IsSqlQueryable
            ? $"ORDER BY {Term.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}"
            : null;

        internal OrderBy(IResource resource) => Resource = resource;

        internal OrderBy(IResource resource, bool descending, string key, ICollection<string> dynamicMembers)
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
            if (IsSqlQueryable) return entities;
            dynamic selector(T i) => Do.Try(() => Term.Evaluate(i), default(object));
            return Ascending ? entities.OrderBy(selector) : entities.OrderByDescending(selector);
        }
    }
}