using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using Newtonsoft.Json.Linq;
using RESTar.Deflection;
using RESTar.Internal;

namespace RESTar.Operations
{
    internal class OrderBy : IFilter
    {
        internal string Key => Term.Key;
        internal bool Descending { get; }
        internal bool Ascending => !Descending;
        internal IResource Resource { get; }
        internal Term Term;
        internal bool IsStarcounterQueryable = true;

        private Func<T1, dynamic> ToSelector<T1>() => item => Do.Try(() => Term.Evaluate(item),
            default(object));

        internal string SQL => IsStarcounterQueryable
            ? $"ORDER BY t.{Term.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}"
            : null;

        internal OrderBy(IResource resource) => Resource = resource;

        internal OrderBy(IResource resource, bool descending, string key, List<string> dynamicMembers)
        {
            Resource = resource;
            Descending = descending;
            Term = Term.ParseInternal(resource.Type, key, resource.IsDynamic, dynamicMembers);
        }

        /// <summary>
        /// Applies the order by operation on an IEnumerable of entities
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (IsStarcounterQueryable) return entities;
            var type = typeof(T);
            if ((entities is IEnumerable<IDictionary<string, dynamic>> || entities is IEnumerable<JObject>) &&
                !(entities is IEnumerable<DDictionary>))
                Term.MakeDynamic();
            else if (type != Resource.Type)
                Term = Term.MakeFromPrototype(Term, type);
            return Ascending ? entities.OrderBy(ToSelector<T>()) : entities.OrderByDescending(ToSelector<T>());
        }
    }
}