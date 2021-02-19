using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Linq;

namespace RESTable.Requests.Processors
{
    /// <inheritdoc cref="List{T}" />
    /// <inheritdoc cref="IProcessor" />
    /// <summary>
    /// Adds properties to entities in an IEnumerable
    /// </summary>
    public class Add : List<Term>, IProcessor
    {
        internal Add(IEntityResource resource, string keys, ICollection<string> dynDomain) => keys
            .ToLower()
            .Split(',')
            .Distinct()
            .Select(key => resource.MakeOutputTerm(key, dynDomain))
            .ForEach(Add);

        private Add(Add other) : base(other) { }
        internal Add GetCopy() => new(this);

        /// <summary>
        /// Adds properties to entities in an IEnumerable
        /// </summary>
        public IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities) => entities?.Select(entity =>
        {
            var jobj = entity.ToJObject();
            ForEach(term =>
            {
                if (jobj[term.Key] != null) return;
                object val = term.Evaluate(entity, out var actualKey);
                jobj[actualKey] = val == null ? null : JToken.FromObject(val, NewtonsoftJsonProvider.Serializer);
            });
            return jobj;
        });
    }
}