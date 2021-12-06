using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.Requests.Processors;

/// <inheritdoc cref="List{T}" />
/// <inheritdoc cref="IProcessor" />
/// <summary>
///     Adds properties to entities in an IEnumerable
/// </summary>
public class Add : List<Term>, IProcessor
{
    public Add(IEnumerable<Term> collection) : base(collection) { }

    /// <summary>
    ///     Adds properties to entities in an IEnumerable
    /// </summary>
    public async IAsyncEnumerable<ProcessedEntity> Apply<T>(IAsyncEnumerable<T> entities, ISerializationMetadata metadata) where T : notnull
    {
        await foreach (var entity in entities.ConfigureAwait(false))
        {
            var dictionary = await entity.MakeProcessedEntity(metadata).ConfigureAwait(false);
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

    internal Add GetCopy()
    {
        return new(this);
    }
}