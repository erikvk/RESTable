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
        public async IAsyncEnumerable<JObject> Apply<T>(IAsyncEnumerable<T> entities)
        {
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            var serializer = jsonProvider.GetSerializer();
            await foreach (var entity in entities.ConfigureAwait(false))
            {
                if (entity is null)
                {
                    continue;
                }
                var jobj = await entity.ToJObject().ConfigureAwait(false);
                foreach (var term in this)
                {
                    if (jobj[term.Key] is not null)
                        continue;
                    var termValue = await term.GetValue(entity).ConfigureAwait(false);
                    var actualKey = termValue.ActualKey;
                    var value = termValue.Value;
                    jobj[actualKey] = value is null ? null : JToken.FromObject(termValue, serializer);
                }
                yield return jobj;
            }
        }
    }
}