using System.Collections.Generic;
using System.Linq;
using RESTar.Serialization;
using static System.StringComparison;

namespace RESTar.Operations
{
    /// <summary>
    /// Searches entities by a given search pattern, and returns entities that match the pattern
    /// </summary>
    public class Search : IFilter
    {
        private string Pattern { get; }
        internal Search(string pattern) => Pattern = pattern;

        /// <summary>
        /// Searches the entities for a given string pattern, and returns only 
        /// those that contains the pattern.
        /// </summary>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (string.IsNullOrWhiteSpace(Pattern)) return entities;
            return entities.Where(e => Serializers.Json.Serialize(e).IndexOf(Pattern, OrdinalIgnoreCase) >= 0);
        }
    }
}