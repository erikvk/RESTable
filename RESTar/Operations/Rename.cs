using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Linq;

namespace RESTar.Operations
{
    /// <summary>
    /// Renames properties in an IEnumerable
    /// </summary>
    public class Rename : Dictionary<Term, string>, IProcessor
    {
        internal Rename(IResource resource, string keys, out ICollection<string> dynamicDomain)
        {
            keys.Split(',').ForEach(keyString =>
            {
                var (termKey, newName) = keyString.TSplit(keys.Contains("->") ? "->" : "-%3E");
                Add(resource.MakeOutputTerm(termKey.ToLower(), null), newName);
            });
            dynamicDomain = Values;
        }

        private JObject Renamed(JObject entity)
        {
            foreach (var pair in this)
            {
                var value = entity.SafeGetNoCase(pair.Key.Key, out var actualKey);
                if (actualKey != null)
                    entity.Remove(actualKey);
                entity[pair.Value] = value;
            }
            return entity;
        }

        /// <summary>
        /// Renames properties in an IEnumerable
        /// </summary>
        public IEnumerable<JObject> Apply<T>(IEnumerable<T> entities) => entities?
            .Select(entity => Renamed(entity.ToJObject()));
    }
}