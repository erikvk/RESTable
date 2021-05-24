using System.Collections.Generic;
using System.Linq;
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
        public Select(IEnumerable<Term> collection) : base(collection) { }

        internal Select GetCopy() => new(this);

        internal JObject Apply<T>(T entity)
        {
            var jobj = new JObject();
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            var serializer = jsonProvider.GetSerializer();
            foreach (var term in this)
            {
                if (jobj[term.Key] is not null) continue;
                var termValue = term.GetValue(entity, out var actualKey);
                jobj[actualKey] = termValue is null ? null : JToken.FromObject(termValue, serializer);
            }
            return jobj;
        }

        /// <summary>
        /// Selects a set of properties from an IEnumerable of entities
        /// </summary>
        public IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities) => entities?.Select(Apply);
    }
}