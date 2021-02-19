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
    /// Selects a set of properties from an IEnumerable of entities
    /// </summary>
    public class Select : List<Term>, IProcessor
    {
        internal Select(IEntityResource resource, string keys, ICollection<string> dynDomain) => keys
            .Split(',')
            .Distinct()
            .Select(key => resource.MakeOutputTerm(key, dynDomain))
            .ForEach(Add);

        private Select(Select other) : base(other) { }
        internal Select GetCopy() => new(this);

        internal JObject Apply<T>(T entity)
        {
            var jobj = new JObject();
            ForEach(term =>
            {
                if (jobj[term.Key] != null) return;
                object val = term.Evaluate(entity, out var actualKey);
                jobj[actualKey] = val == null ? null : JToken.FromObject(val, NewtonsoftJsonProvider.Serializer);
            });
            return jobj;
        }

        /// <summary>
        /// Selects a set of properties from an IEnumerable of entities
        /// </summary>
        public IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities) => entities?.Select(Apply);
    }
}