using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using static System.StringComparison;

namespace RESTable.Requests.Filters;

/// <summary>
///     Searches entities by a given search pattern, and returns entities that match the pattern
/// </summary>
public class Search : IFilter
{
    /// <summary>
    ///     Creates a new Search filter based on a given search pattern
    /// </summary>
    public Search(string? pattern)
    {
        var parts = pattern?.Split(',');
        Pattern = parts?.ElementAtOrDefault(0);
        if (string.IsNullOrWhiteSpace(Pattern))
            throw new ArgumentException("Invalid search pattern. Cannot be null or whitespace");
        Selector = parts?.ElementAtOrDefault(1);
        if (Selector == "") Selector = null;
        switch (parts?.ElementAtOrDefault(2))
        {
            case "":
            case null:
            case "CI":
            case "ci":
                IgnoreCase = true;
                break;
            case "CS":
            case "cs":
                IgnoreCase = false;
                break;
            case var other:
                throw new ArgumentException($"Invalid case sensitivity argument '{other}'. Must be " +
                                            "'CS' (case sensitive) or 'CI' (case insensitive)");
        }
    }

    private Search(string? pattern, string? selector, bool ignoreCase)
    {
        Pattern = pattern;
        Selector = selector;
        IgnoreCase = ignoreCase;
    }

    /// <summary>
    ///     The case pattern to search for
    /// </summary>
    public string? Pattern { get; }

    /// <summary>
    ///     The selector to use before searching
    /// </summary>
    protected string? Selector { get; }

    /// <summary>
    ///     Should the search ignore case?
    /// </summary>
    protected bool IgnoreCase { get; }

    /// <summary>
    ///     Searches the entities for a given case insensitive string pattern, and returns only
    ///     those that contain the pattern.
    /// </summary>
    public virtual IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
    {
        if (Pattern is null)
            return entities;
        var comparison = IgnoreCase ? OrdinalIgnoreCase : Ordinal;
        var jsonProvider = ApplicationServicesAccessor.GetRequiredService<IJsonProvider>();
        if (Selector is null) return entities.Where(e => jsonProvider.Serialize(e).IndexOf(Pattern, comparison) >= 0);
        return entities.Where(entity =>
        {
            var jsonElement = jsonProvider.ToJsonElement(entity);
            var selectedValue = jsonElement.GetProperty(Selector, OrdinalIgnoreCase)?.Value;
            var matchingPropertyValue = !selectedValue.HasValue ? null : jsonProvider.ToObject<object>(selectedValue.Value);
            return matchingPropertyValue?.ToString()?.IndexOf(Pattern, comparison) >= 0;
        });
    }

    internal Search GetCopy()
    {
        return new(Pattern, Selector, IgnoreCase);
    }

    internal string GetValueLiteral()
    {
        return $"{Pattern},{Selector},{(IgnoreCase ? "CI" : "CS")}";
    }
}