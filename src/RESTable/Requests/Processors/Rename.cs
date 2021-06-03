using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.Meta;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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

        private JsonSerializer Serializer { get; }

        private Rename(Rename other) : base(other)
        {
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            Serializer = jsonProvider.GetSerializer();
        }

        internal Rename GetCopy() => new(this);

        private async ValueTask<JObject> Renamed(JObject entity)
        {
            foreach (var pair in this)
            {
                var (key, newName) = pair;
                var value = entity.GetValue(key.Key, StringComparison.OrdinalIgnoreCase);
                if (value is null)
                {
                    var termValue = await key.GetValue(entity).ConfigureAwait(false);
                    entity[newName] = termValue.Value is null ? null : JToken.FromObject(termValue.Value, Serializer);
                    continue;
                }
                var property = (JProperty?) value.Parent;
                var actualKey = property?.Name;
                if (actualKey is not null)
                    entity.Remove(actualKey);
                entity[newName] = value;
            }
            return entity;
        }

        /// <summary>
        /// Renames properties in an IEnumerable
        /// </summary>
        public async IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities)
        {
            await foreach (var entity in entities)
            {
                if (entity is null) throw new ArgumentNullException(nameof(entities));
                var jobject = await entity.ToJObject().ConfigureAwait(false);
                yield return await Renamed(jobject).ConfigureAwait(false);
            }
        }
    }
}