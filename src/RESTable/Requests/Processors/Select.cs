using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Processors
{
    /// <inheritdoc cref="List{T}" />
    /// <inheritdoc cref="IProcessor" /> 
    /// <summary>
    /// Selects a set of properties from an IEnumerable of entities
    /// </summary>
    public class Select : List<Term>, IProcessor
    {
        private JsonSerializer Serializer { get; }

        public Select(IEnumerable<Term> collection) : base(collection)
        {
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            Serializer = jsonProvider.GetSerializer();
        }

        internal Select GetCopy() => new(this);

        private async ValueTask<JObject> Apply<T>(T entity)
        {
            if (entity is null) throw new ArgumentNullException(nameof(entity));
            var jobj = new JObject();
            foreach (var term in this)
            {
                if (jobj[term.Key] is not null) continue;
                var termValue = await term.GetValue(entity).ConfigureAwait(false);
                var actualKey = termValue.ActualKey;
                jobj[actualKey] = termValue.Value is null ? null : JToken.FromObject(termValue.Value, Serializer);
            }
            return jobj;
        }

        /// <summary>
        /// Selects a set of properties from an IEnumerable of entities
        /// </summary>
        public async IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities)
        {
            await foreach (var entity in entities)
            {
                yield return await Apply(entity).ConfigureAwait(false);
            }
        }
    }
}