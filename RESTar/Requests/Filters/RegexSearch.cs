using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RESTar.Admin;
using RESTar.ContentTypeProviders;

namespace RESTar.Requests.Filters
{
    /// <inheritdoc />
    /// <summary>
    /// Searches entities by a given search pattern, and returns entities that match the pattern
    /// </summary>
    public class RegexSearch : Search
    {
        /// <inheritdoc />
        public RegexSearch(string pattern) : base(pattern) { }

        /// <inheritdoc />
        /// <summary>
        /// Searches the entities using Pattern as a regex pattern, and returns only 
        /// those that match the pattern.
        /// </summary>
        public override IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            if (string.IsNullOrWhiteSpace(Pattern)) return entities;
            var formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
            return entities.Where(e => Regex.IsMatch(Providers.Json.Serialize(e, formatting), Pattern));
        }
    }
}