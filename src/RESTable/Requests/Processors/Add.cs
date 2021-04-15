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
            var jobj = entity.ToJObject();
            ForEach(term =>
            {
                if (jobj[term.Key] != null) return;
                object val = term.GetValue(entity, out var actualKey);
                jobj[actualKey] = val == null ? null : JToken.FromObject(val, jsonProvider.GetSerializer());
            });
            return jobj;
        });
    }
}