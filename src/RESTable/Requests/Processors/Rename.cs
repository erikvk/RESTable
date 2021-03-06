using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTable.Meta;
using RESTable.Linq;

namespace RESTable.Requests.Processors
{
    /// <inheritdoc cref="Dictionary{TKey,TValue}" />
    /// /// <inheritdoc cref="IProcessor" />
    /// <summary>
    /// Renames properties in an IEnumerable
    /// </summary>
    public class Rename : Dictionary<Term, string>, IProcessor
    {
        internal Rename(IEnumerable<(Term term, string newName)> terms, out ICollection<string> dynamicDomain)
        {
            foreach (var (term, newName) in terms) 
                Add(term, newName);
            dynamicDomain = Values;
        }

        private Rename(Rename other) : base(other) { }
        internal Rename GetCopy() => new(this);

        private JObject Renamed(JObject entity)
        {
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            this.ForEach(pair =>
            {
                var (key, newName) = pair;
                var value = entity.GetValue(key.Key, StringComparison.OrdinalIgnoreCase);
                if (value == null)
                {
                    value = key.Evaluate(entity, out _);
                    entity[newName] = value == null ? null : JToken.FromObject(value, jsonProvider.GetSerializer());
                    return;
                }
                var property = (JProperty) value.Parent;
                var actualKey = property?.Name;
                if (actualKey != null)
                    entity.Remove(actualKey);
                entity[newName] = value;
            });
            return entity;
        }

        /// <summary>
        /// Renames properties in an IEnumerable
        /// </summary>
        public IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities) => entities?.Select(entity => Renamed(entity.ToJObject()));
    }
}