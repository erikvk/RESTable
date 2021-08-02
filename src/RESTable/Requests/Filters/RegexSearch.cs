using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTable.Meta;
using static System.StringComparison;

namespace RESTable.Requests.Filters
{
    /// <inheritdoc />
    /// <summary>
    /// Searches entities by a given search pattern, and returns entities that match the pattern
    /// </summary>
    public class RegexSearch : Search
    {
        /// <inheritdoc />
        public RegexSearch(string? pattern) : base(pattern) { }

        /// <inheritdoc />
        /// <summary>
        /// Searches the entities using Pattern as a regex pattern, and returns only 
        /// those that match the pattern.
        /// </summary>
        public override IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities)
        {
            if (Pattern is null)
                return entities;
            var jsonProvider = ApplicationServicesAccessor.JsonProvider;
            var options = IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            if (Selector is null)
            {
                return entities.Where(e => Regex.IsMatch(jsonProvider.Serialize(e), Pattern, options));
            }
            return entities.Where(entity =>
            {
                var jsonElement = jsonProvider.ToJsonElement(entity);
                var selectedValue = jsonElement.GetProperty(Selector, OrdinalIgnoreCase)?.Value;
                var matchingPropertyValue = !selectedValue.HasValue ? null : jsonProvider.ToObject<object>(selectedValue.Value);
                return matchingPropertyValue?.ToString() is string s && Regex.IsMatch(s, Pattern, options);
            });
        }
    }
}