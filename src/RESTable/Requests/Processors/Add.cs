using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTable.Meta;

namespace RESTable.Requests.Processors
{
    /// <inheritdoc cref="List{T}" />
    /// <inheritdoc cref="IProcessor" />
    /// <summary>
    /// Adds properties to entities in an IEnumerable
    /// </summary>
    public class Add : List<Term>, IProcessor
    {
        public Add(IEnumerable<Term> collection) : base(collection) { }

        internal Add GetCopy() => new(this);

        /// <summary>
        /// Adds properties to entities in an IEnumerable
        /// </summary>
        public IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities) => entities?.Select(entity =>
        {
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            var serializer = jsonProvider.GetSerializer();
            var jobj = entity.ToJObject();
            foreach (var term in this)
            {
                if (jobj[term.Key] != null) continue;
                var termValue = term.GetValue(entity, out var actualKey);
                jobj[actualKey] = termValue == null ? null : JToken.FromObject(termValue, serializer);
            }
            return jobj;
        });
    }
}