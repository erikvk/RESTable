using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async IAsyncEnumerable<object> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
        {
            await foreach (var entity in entities.ConfigureAwait(false))
            {
                var dictionary = await entity.MakeShallowDynamic().ConfigureAwait(false);
                foreach (var term in this)
                {
                    if (dictionary.ContainsKey(term.Key))
                        continue;
                    var termValue = await term.GetValue(entity).ConfigureAwait(false);
                    dictionary[termValue.ActualKey] = termValue.Value;
                }
                yield return dictionary;
            }
        }
    }
}