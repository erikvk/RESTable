using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.Requests.Filters
{
    /// <summary>
    /// Applies a distinct filtering to the inputted entities
    /// </summary>
    public class Distinct : IFilter
    {
        private IEqualityComparer<JsonElement> EqualityComparer { get; }

        public Distinct()
        {
            EqualityComparer = new JsonElementComparer();
        }

        /// <summary>
        /// Applies the distinct filtering
        /// </summary>
        public IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
        {
            return DistinctIterator(entities);
        }

        private async IAsyncEnumerable<TSource> DistinctIterator<TSource>(IAsyncEnumerable<TSource> source) where TSource : notnull
        {
            var set = new HashSet<JsonElement>(EqualityComparer);
            await foreach (var element in source.ConfigureAwait(false))
            {
                if (element is null) throw new ArgumentNullException(nameof(source));
                var jobject = element.ToJsonElement();
                if (set.Add(jobject))
                {
                    yield return element;
                }
            }
        }
    }
}