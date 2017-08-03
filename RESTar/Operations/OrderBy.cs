using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;

namespace RESTar.Operations
{
    internal class OrderBy : IFilter
    {
        internal string Key => Term.Key;
        internal bool Descending { get; }
        internal bool Ascending => !Descending;
        internal IResource Resource { get; }
        internal readonly Term Term;
        internal bool IsStarcounterQueryable = true;

        internal string SQL => IsStarcounterQueryable
            ? $"ORDER BY t.{Term.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}"
            : null;

        internal OrderBy(IResource resource) => Resource = resource;

        internal OrderBy(IResource resource, bool descending, string key, IEnumerable<string> dynamicMembers)
        {
            Resource = resource;
            Descending = descending;
            Term = dynamicMembers == null
                ? resource.MakeTerm(key, resource.IsDynamic)
                : Term.Parse(resource.Type, key, resource.IsDynamic, dynamicMembers);
        }

        /// <summary>
        /// Applies the order by operation on an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (IsStarcounterQueryable) return entities;
            if (typeof(T) == typeof(JObject))
                Term.MakeDynamic();
            var selector = Term.ToSelector<T>();
            return Ascending ? entities.OrderBy(selector) : entities.OrderByDescending(selector);
        }
    }
}