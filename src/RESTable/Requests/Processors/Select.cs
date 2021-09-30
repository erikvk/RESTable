using System.Collections.Generic;
using System.Threading.Tasks;
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

        private async ValueTask<ProcessedEntity> Apply<T>(T entity) where T : notnull
        {
            var dictionary = new ProcessedEntity();
            foreach (var term in this)
            {
                if (dictionary.ContainsKey(term.Key))
                    continue;
                var termValue = await term.GetValue(entity).ConfigureAwait(false);
                dictionary[termValue.ActualKey] = termValue.Value;
            }
            return dictionary;
        }

        /// <summary>
        /// Selects a set of properties from an IEnumerable of entities
        /// </summary>
        public async IAsyncEnumerable<ProcessedEntity> Apply<T>(IAsyncEnumerable<T> entities, ISerializationMetadata metadata) where T : notnull
        {
            await foreach (var entity in entities)
            {
                yield return await Apply(entity).ConfigureAwait(false);
            }
        }
    }
}