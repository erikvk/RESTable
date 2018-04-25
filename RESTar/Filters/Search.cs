using System.Collections.Generic;
using System.Linq;
using RESTar.Serialization;
using static System.StringComparison;

namespace RESTar.Filters
{
    /// <summary>
    /// Searches entities by a given search pattern, and returns entities that match the pattern
    /// </summary>
    public class Search : IFilter
    {
        /// <summary>
        /// The case insensitive pattern to search for
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Creates a new Search filter based on a given case insensitive search pattern
        /// </summary>
        /// <param name="pattern"></param>
        public Search(string pattern) => Pattern = pattern;

        /// <summary>
        /// Searches the entities for a given case insensitive string pattern, and returns only 
        /// those that contain the pattern.
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (string.IsNullOrWhiteSpace(Pattern)) return entities;
            return entities.Where(e => Serializers.Json.Serialize(e).IndexOf(Pattern, OrdinalIgnoreCase) >= 0);
        }
    }
}