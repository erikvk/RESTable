using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async IAsyncEnumerable<JObject?>? Apply<T>(IAsyncEnumerable<T>? entities)
        {
            if (entities is null)
                yield break;
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            var serializer = jsonProvider.GetSerializer();
            await foreach (var entity in entities.ConfigureAwait(false))
            {
                if (entity is null)
                {
                    yield return null;
                    continue;
                }
                var jobj = entity.ToJObject();
                foreach (var term in this)
                {
                    if (jobj[term.Key] is not null) continue;
                    var termValue = term.GetValue(entity, out var actualKey);
                    jobj[actualKey] = termValue is null ? null : JToken.FromObject(termValue, serializer);
                }
                yield return jobj;
            }
        }
    }
}