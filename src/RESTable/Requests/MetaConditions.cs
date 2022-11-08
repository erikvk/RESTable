using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Requests.Filters;
using RESTable.Requests.Processors;
using RESTable.Results;
using static RESTable.ErrorCodes;
using static RESTable.Requests.Operators;

#pragma warning disable 612

namespace RESTable.Requests;

/// <summary>
///     Contains the metaconditions for a request
/// </summary>
public sealed class MetaConditions
{
    private bool Empty;

    public MetaConditions()
    {
        CanUseExternalCounter = true;
        Empty = true;
        SafePost = null!;
        Search = null!;
        Distinct = null!;
        Rename = null!;
        Add = null!;
        Select = null!;
        OrderBy = null!;
        Offset = Offset.NoOffset;
        Limit = Limit.NoLimit;
        ProcessorsList = new List<IProcessor>(3);
    }

    /// <summary>
    ///     Is this request unsafe?
    /// </summary>
    public bool Unsafe { get; set; }

    /// <summary>
    ///     The limit by which the request's response body entity count should be restricted to
    /// </summary>
    public Limit Limit { get; set; }

    /// <summary>
    ///     An offset in the request's entities, on which enumeration will start when creating the
    ///     response
    /// </summary>
    public Offset Offset { get; set; }

    /// <summary>
    ///     The OrderBy filter to apply to the output from this request
    /// </summary>
    public OrderBy? OrderBy { get; set; }

    /// <summary>
    ///     The Select processor to apply to the output from this request
    /// </summary>
    public Select? Select { get; set; }

    /// <summary>
    ///     The Add processor to apply to the output from this request
    /// </summary>
    public Add? Add { get; set; }

    /// <summary>
    ///     The Renam processor to apply to the output from this request
    /// </summary>
    public Rename? Rename { get; set; }

    /// <summary>
    ///     The Distinct processor to apply to the output from this request
    /// </summary>
    public Distinct? Distinct { get; set; }

    /// <summary>
    ///     The search filter to apply to the output from this request
    /// </summary>
    public Search? Search { get; set; }

    /// <summary>
    ///     The term to use for safepost
    /// </summary>
    public string? SafePost { get; set; }

    private List<IProcessor> ProcessorsList { get; }

    public IReadOnlyList<IProcessor> Processors => ProcessorsList.AsReadOnly();

    public bool HasProcessors => ProcessorsList.Count > 0;

    internal bool CanUseExternalCounter { get; private set; }

    internal static MetaConditions Parse(IReadOnlyCollection<IUriCondition> uriMetaConditions, IEntityResource resource, TermFactory termFactory)
    {
        if (uriMetaConditions.Count == 0)
            return new MetaConditions();
        var renames = uriMetaConditions.Where(c => c.Key.EqualsNoCase("rename"));
        var others = uriMetaConditions.Where(c => !c.Key.EqualsNoCase("rename"));
        MetaConditions metaConditions = new() { Empty = false };
        ICollection<string>? dynamicDomain = default;

        void make(IEnumerable<IUriCondition> conditions)
        {
            foreach (var condition in conditions)
            {
                var (key, op, valueLiteral) = (condition.Key, condition.Operator, condition.ValueLiteral);
                if (op != EQUALS)
                    throw new InvalidSyntax(InvalidMetaConditionOperator,
                        "Invalid operator for meta-condition. One and only one '=' is allowed");
                if (!Enum.TryParse(key, true, out RESTableMetaCondition metaCondition))
                    throw new InvalidSyntax(InvalidMetaConditionKey, $"Invalid meta-condition '{key}'. Available meta-conditions: " +
                                                                     $"{string.Join(", ", EnumMember<RESTableMetaCondition>.Names)}");

                var expectedType = metaCondition.GetExpectedType();

                if (valueLiteral == "∞")
                    valueLiteral = int.MaxValue.ToString();
                else if (valueLiteral == "-∞")
                    valueLiteral = int.MinValue.ToString();

                var (first, length) = (valueLiteral.FirstOrDefault(), valueLiteral.Length);

                switch (first)
                {
                    case '\'':
                    case '\"':
                        if (length > 1 && valueLiteral[length - 1] == first)
                            valueLiteral = valueLiteral.Substring(1, length - 2);
                        break;
                }

                object value;
                try
                {
                    value = Convert.ChangeType(valueLiteral, expectedType);
                }
                catch
                {
                    throw new InvalidSyntax(InvalidMetaConditionValueType,
                        $"Invalid data type for value '{valueLiteral}' assigned to meta-condition '{key}'. Expected {GetTypeString(expectedType)}.");
                }
                switch (metaCondition)
                {
                    case RESTableMetaCondition.Unsafe:
                        metaConditions.Unsafe = (bool) value;
                        break;
                    case RESTableMetaCondition.Limit:
                        metaConditions.Limit = (int) value;
                        break;
                    case RESTableMetaCondition.Offset:
                        metaConditions.Offset = (int) value;
                        break;
                    case RESTableMetaCondition.Order_asc:
                    {
                        var term = termFactory.MakeOutputTerm(resource, (string) value, dynamicDomain);
                        metaConditions.OrderBy = new OrderByAscending(resource, term);
                        break;
                    }
                    case RESTableMetaCondition.Order_desc:
                    {
                        var term = termFactory.MakeOutputTerm(resource, (string) value, dynamicDomain);
                        metaConditions.OrderBy = new OrderByDescending(resource, term);
                        break;
                    }
                    case RESTableMetaCondition.Select:
                    {
                        var selectKeys = (string) value;
                        var domain = dynamicDomain;
                        var terms = selectKeys
                            .Split(',')
                            .Distinct()
                            .Select(selectKey => termFactory.MakeOutputTerm(resource, selectKey, domain));
                        metaConditions.Select = new Select(terms);
                        break;
                    }
                    case RESTableMetaCondition.Add:
                    {
                        var addKeys = (string) value;
                        var domain = dynamicDomain;
                        var terms = addKeys
                            .ToLower()
                            .Split(',')
                            .Distinct()
                            .Select(addKey => termFactory.MakeOutputTerm(resource, addKey, domain));
                        metaConditions.Add = new Add(terms);
                        break;
                    }
                    case RESTableMetaCondition.Rename:
                    {
                        var renameKeys = (string) value;
                        var terms = renameKeys.Split(',').Select(keyString =>
                        {
                            var (termKey, newName) = keyString.TupleSplit(renameKeys.Contains("->") ? "->" : "-%3E");
                            return
                            (
                                termFactory.MakeOutputTerm(resource, termKey.ToLowerInvariant(), null),
                                newName ?? throw new ArgumentException("Missing new name in rename")
                            );
                        });
                        metaConditions.Rename = new Rename(terms, out dynamicDomain);
                        break;
                    }
                    case RESTableMetaCondition.Distinct:
                        if ((bool) value)
                            metaConditions.Distinct = new Distinct();
                        break;
                    case RESTableMetaCondition.Search:
                        metaConditions.Search = new Search((string) value);
                        break;
                    case RESTableMetaCondition.Search_regex:
                        metaConditions.Search = new RegexSearch((string) value);
                        break;
                    case RESTableMetaCondition.Safepost:
                        metaConditions.SafePost = (string) value;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        make(renames);
        make(others);

        metaConditions.CanUseExternalCounter = metaConditions.Search is null
                                               && metaConditions.Distinct is null
                                               && metaConditions.Limit.Number == -1
                                               && metaConditions.Offset.Number == 0;

        if (metaConditions.Add is not null)
            metaConditions.ProcessorsList.Add(metaConditions.Add);
        if (metaConditions.Rename is not null)
            metaConditions.ProcessorsList.Add(metaConditions.Rename);
        if (metaConditions.Select is not null)
            metaConditions.ProcessorsList.Add(metaConditions.Select);

        if (metaConditions.OrderBy is not null)
            if (metaConditions.Rename?.Any(p => p.Key.Key.EqualsNoCase(metaConditions.OrderBy.Key)) == true
                && !metaConditions.Rename.Any(p => p.Value.EqualsNoCase(metaConditions.OrderBy.Key)))
                throw new InvalidSyntax(InvalidMetaConditionSyntax,
                    $"The {(metaConditions.OrderBy is OrderByAscending ? RESTableMetaCondition.Order_asc : RESTableMetaCondition.Order_desc)} " +
                    "meta-condition cannot refer to a property x that is to be renamed " +
                    "unless some other property is renamed to x");

        if (metaConditions.Select is not null && metaConditions.Rename is not null)
            if (metaConditions.Select.Any(pc => metaConditions.Rename.Any(p => p.Key.Key.EqualsNoCase(pc.Key)) &&
                                                !metaConditions.Rename.Any(p => p.Value.EqualsNoCase(pc.Key))))
                throw new InvalidSyntax(InvalidMetaConditionSyntax,
                    "A 'Select' meta-condition cannot refer to a property x that is " +
                    "to be renamed unless some other property is renamed to x. Use the " +
                    "new property name instead.");

        return metaConditions;
    }

    private static string? GetTypeString(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => "a string",
            _ when type == typeof(int) => "an integer",
            _ when type == typeof(bool) => "a boolean",
            _ => null
        };
    }

    /// <summary>
    ///     Converts the MetaConditions object into a collection of IUriCondition instances
    /// </summary>
    public IEnumerable<IUriCondition> GetEnumerable()
    {
        if (Unsafe)
            yield return new UriCondition(RESTableMetaCondition.Unsafe, "true");
        if (Limit.Number > -1)
            yield return new UriCondition(RESTableMetaCondition.Limit, Limit.Number.ToString());
        if (Offset.Number == int.MinValue)
            yield return new UriCondition(RESTableMetaCondition.Offset, "-∞");
        else if (Offset.Number == int.MaxValue)
            yield return new UriCondition(RESTableMetaCondition.Offset, "∞");
        else if (Offset.Number != 0)
            yield return new UriCondition(RESTableMetaCondition.Offset, Offset.Number.ToString());
        if (OrderBy is OrderByDescending)
            yield return new UriCondition(RESTableMetaCondition.Order_desc, OrderBy.Term.ToString());
        else if (OrderBy is not null)
            yield return new UriCondition(RESTableMetaCondition.Order_asc, OrderBy.Term.ToString());
        if (Select is not null)
            yield return new UriCondition(RESTableMetaCondition.Select, string.Join(",", Select));
        if (Add is not null)
            yield return new UriCondition(RESTableMetaCondition.Add, string.Join(",", Add));
        if (Rename is not null)
            yield return new UriCondition(RESTableMetaCondition.Rename, string.Join(",", Rename.Select(r => $"{r.Key}->{r.Value}")));
        if (Distinct is not null)
            yield return new UriCondition(RESTableMetaCondition.Distinct, "true");
        if (Search is RegexSearch)
            yield return new UriCondition(RESTableMetaCondition.Search_regex, Search.GetValueLiteral());
        else if (Search is not null)
            yield return new UriCondition(RESTableMetaCondition.Search, Search.GetValueLiteral());
        if (SafePost is not null)
            yield return new UriCondition(RESTableMetaCondition.Safepost, SafePost);
    }

    internal MetaConditions GetCopy()
    {
        var copy = new MetaConditions
        {
            Unsafe = Unsafe,
            Limit = Limit,
            Offset = Offset,
            OrderBy = OrderBy?.GetCopy(),
            Select = Select?.GetCopy(),
            Add = Add?.GetCopy(),
            Rename = Rename?.GetCopy(),
            Distinct = Distinct,
            Search = Search?.GetCopy(),
            SafePost = SafePost,
            Empty = Empty
        };
        copy.CanUseExternalCounter = copy.Search is null && copy.Distinct is null && copy.Limit.Number == -1 && copy.Offset.Number == 0;
        return copy;
    }
}
