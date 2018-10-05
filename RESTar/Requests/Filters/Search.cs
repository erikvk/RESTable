using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RESTar.Admin;
using RESTar.ContentTypeProviders;

namespace RESTar.Requests.Filters
{
    /// <summary>
    /// Searches entities by a given search pattern, and returns entities that match the pattern
    /// </summary>
    public class Search : IFilter
    {
        /// <summary>
        /// The case pattern to search for
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Creates a new Search filter based on a given search pattern
        /// </summary>
        /// <param name="pattern"></param>
        public Search(string pattern) => Pattern = pattern;

        /// <summary>
        /// Searches the entities for a given case insensitive string pattern, and returns only 
        /// those that contain the pattern.
        /// </summary>
        public virtual IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (string.IsNullOrWhiteSpace(Pattern)) return entities;
            var formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
            return entities.Where(e => Providers.Json.Serialize(e, formatting).IndexOf(Pattern, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}