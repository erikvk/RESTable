using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

namespace RESTable.Requests.Filters;

/// <summary>
///     Applies a distinct filtering to the inputted entities
/// </summary>
public class Distinct : IFilter
{
    public Distinct()
    {
        EqualityComparer = new JsonElementComparer();
        JsonProvider = ApplicationServicesAccessor.GetRequiredService<IJsonProvider>();
    }

    private IEqualityComparer<JsonElement> EqualityComparer { get; }
    private IJsonProvider JsonProvider { get; }

    /// <summary>
    ///     Applies the distinct filtering
    /// </summary>
    public IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
    {
        return DistinctIterator(entities);
    }

    private async IAsyncEnumerable<TSource> DistinctIterator<TSource>(IAsyncEnumerable<TSource> source) where TSource : notnull
    {
        var set = new HashSet<JsonElement>(EqualityComparer);
        await foreach (var item in source.ConfigureAwait(false))
        {
            if (item is null) throw new ArgumentNullException(nameof(source));
            var jsonElement = JsonProvider.ToJsonElement(item);
            if (set.Add(jsonElement)) yield return item;
        }
    }
}