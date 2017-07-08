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
        public string Key => PropertyChain.Key;

        internal void SetStaticKey(string key) => PropertyChain = PropertyChain
            .GetOrMake(Resource, key, Resource.IsDynamic);

        public bool Descending;
        public bool Ascending => !Descending;

        internal readonly IResource Resource;
        internal PropertyChain PropertyChain;

        internal bool IsStarcounterQueryable = true;
        private Func<T1, dynamic> ToSelector<T1>() => item => Do.Try(() => PropertyChain.Get(item), default(object));

        internal string SQL => IsStarcounterQueryable
            ? $"ORDER BY t.{PropertyChain.DbKey.Fnuttify()} {(Descending ? "DESC" : "ASC")}"
            : null;

        internal OrderBy(IResource resource) => Resource = resource;

        internal OrderBy(IResource resource, bool descending, string key, List<string> dynamicMembers)
        {
            Resource = resource;
            Descending = descending;
            PropertyChain = PropertyChain.ParseInternal(resource, key, resource.IsDynamic, dynamicMembers);
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (IsStarcounterQueryable) return entities;
            var type = typeof(T);
            if ((entities is IEnumerable<IDictionary<string, dynamic>> || entities is IEnumerable<JObject>) &&
                !(entities is IEnumerable<DDictionary>))
                PropertyChain.MakeDynamic();
            else if (type != Resource.TargetType)
                PropertyChain = PropertyChain.MakeFromPrototype(PropertyChain, type);
            return Ascending ? entities.OrderBy(ToSelector<T>()) : entities.OrderByDescending(ToSelector<T>());
        }
    }
}