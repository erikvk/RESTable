using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RESTable.Requests.Filters
{
    /// <summary>
    /// Applies a distinct filtering to the inputted entities
    /// </summary>
    public class Distinct : IFilter
    {
        /// <summary>
        /// Applies the distinct filtering
        /// </summary>
        public IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : class
        {
            if (entities == null) return null;
            return DistinctIterator(entities);
        }

        private static async IAsyncEnumerable<TSource> DistinctIterator<TSource>(IAsyncEnumerable<TSource> source)
        {
            var set = new HashSet<JObject>(JToken.EqualityComparer);
            await foreach (var element in source)
            {
                if (set.Add(element.ToJObject()))
                {
                    yield return element;
                }
            }
        }
    }
}