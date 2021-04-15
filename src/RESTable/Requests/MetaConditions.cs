using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Requests.Filters;
using RESTable.Requests.Processors;
using RESTable.Results;
using RESTable.Linq;
using static RESTable.ErrorCodes;
using static RESTable.Requests.Operators;

#pragma warning disable 612

namespace RESTable.Requests
{
    /// <summary>
    /// Contains the metaconditions for a request
    /// </summary>
    public sealed class MetaConditions
    {
        /// <summary>
        /// Is this request unsafe?
        /// </summary>
        public bool Unsafe { get; set; }

        /// <summary>
        /// The limit by which the request's response body entity count should be restricted to
        /// </summary>
        public Limit Limit { get; set; } = Limit.NoLimit;

        /// <summary>
        /// An offset in the request's entities, on which enumeration will start when creating the 
        /// response
        /// </summary>
        public Offset Offset { get; set; } = Offset.NoOffset;

        /// <summary>
        /// The OrderBy filter to apply to the output from this request
        /// </summary>
        public OrderBy OrderBy { get; set; }

        /// <summary>
        /// The Select processor to apply to the output from this request
        /// </summary>
        public Select Select { get; set; }

        /// <summary>
        /// The Add processor to apply to the output from this request
        /// </summary>
        public Add Add { get; set; }

        /// <summary>
        /// The Renam processor to apply to the output from this request
        /// </summary>
        public Rename Rename { get; set; }

        /// <summary>
        /// The Distinct processor to apply to the output from this request
        /// </summary>
        public Distinct Distinct { get; set; }

        /// <summary>
        /// The search filter to apply to the output from this request
        /// </summary>
        public Search Search { get; set; }

        /// <summary>
        /// The term to use for safepost
        /// </summary>
        public string SafePost { get; set; }

        internal bool Empty = true;

        internal IProcessor[] Processors { get; private set; }
        internal bool HasProcessors { get; private set; }
        internal bool CanUseExternalCounter { get; private set; } = true;

        private static string AllMetaConditions =>
            $"{string.Join(", ", Enum.GetNames(typeof(RESTableMetaCondition)).Except(new[] {"New", "Delete"}))}";

        internal static MetaConditions Parse(IReadOnlyCollection<IUriCondition> uriMetaConditions, IEntityResource resource, TermFactory termFactory)
        {
            if (uriMetaConditions?.Any() != true) return null;
            var renames = uriMetaConditions.Where(c => c.Key.EqualsNoCase("rename"));
            var regular = uriMetaConditions.Where(c => !c.Key.EqualsNoCase("rename"));
            var mc = new MetaConditions {Empty = false};
            ICollection<string> dynamicDomain = default;

            void make(IEnumerable<IUriCondition> conds) => conds.ForEach(cond =>
            {
                var (key, op, valueLiteral) = (cond.Key, cond.Operator, cond.ValueLiteral);
                if (op != EQUALS)
                    throw new InvalidSyntax(InvalidMetaConditionOperator,
                        "Invalid operator for meta-condition. One and only one '=' is allowed");
                if (!Enum.TryParse(key, true, out RESTableMetaCondition metaCondition))
                    throw new InvalidSyntax(InvalidMetaConditionKey,
                        $"Invalid meta-condition '{key}'. Available meta-conditions: {AllMetaConditions}");

                var expectedType = metaCondition.GetExpectedType();

                switch (valueLiteral)
                {
                    case null:
                    case "null":
                    case "": return;
                    case "∞":
                        valueLiteral = int.MaxValue.ToString();
                        break;
                    case "-∞":
                        valueLiteral = int.MinValue.ToString();
                        break;
                }

                var (first, length) = (valueLiteral.FirstOrDefault(), valueLiteral.Length);

                switch (first)
                {
                    case '\'':
                    case '\"':
                        if (length > 1 && valueLiteral[length - 1] == first)
                            valueLiteral = valueLiteral.Substring(1, length - 2);
                        break;
                }

                dynamic value;
                try
                {
                    value = Convert.ChangeType(valueLiteral, expectedType);
                }
                catch
                {
                    throw new InvalidSyntax(InvalidMetaConditionValueType,
                        $"Invalid data type assigned to meta-condition '{key}'. Expected {GetTypeString(expectedType)}.");
                }
                switch (metaCondition)
                {
                    case RESTableMetaCondition.Unsafe:
                        mc.Unsafe = value;
                        break;
                    case RESTableMetaCondition.Limit:
                        mc.Limit = (Limit) (int) value;
                        break;
                    case RESTableMetaCondition.Offset:
                        mc.Offset = (Offset) (int) value;
                        break;
                    case RESTableMetaCondition.Order_asc:
                    {
                        var term = termFactory.MakeOutputTerm(resource, (string) value, dynamicDomain);
                        mc.OrderBy = new OrderByAscending(resource, term);
                        break;
                    }
                    case RESTableMetaCondition.Order_desc:
                    {
                        var term = termFactory.MakeOutputTerm(resource, (string) value, dynamicDomain);
                        mc.OrderBy = new OrderByDescending(resource, term);
                        break;
                    }
                    case RESTableMetaCondition.Select:
                    {
                        var selectKeys = (string) value;
                        var terms = selectKeys
                            .Split(',')
                            .Distinct()
                            .Select(selectKey => termFactory.MakeOutputTerm(resource, selectKey, dynamicDomain));
                        mc.Select = new Select(terms);
                        break;
                    }
                    case RESTableMetaCondition.Add:
                    {
                        var addKeys = (string) value;
                        var terms = addKeys
                            .ToLower()
                            .Split(',')
                            .Distinct()
                            .Select(addKey => termFactory.MakeOutputTerm(resource, addKey, dynamicDomain));
                        mc.Add = new Add(terms);
                        break;
                    }
                    case RESTableMetaCondition.Rename:
                    {
                        var renameKeys = (string) value;
                        var terms = renameKeys.Split(',').Select(keyString =>
                        {
                            var (termKey, newName) = keyString.TSplit(renameKeys.Contains("->") ? "->" : "-%3E");
                            return
                            (
                                termFactory.MakeOutputTerm(resource, termKey.ToLowerInvariant(), null),
                                newName
                            );
                        });
                        mc.Rename = new Rename(terms, out dynamicDomain);
                        break;
                    }
                    case RESTableMetaCondition.Distinct:
                        if ((bool) value)
                            mc.Distinct = new Distinct();
                        break;
                    case RESTableMetaCondition.Search:
                        mc.Search = new Search((string) value);
                        break;
                    case RESTableMetaCondition.Search_regex:
                        mc.Search = new RegexSearch((string) value);
                        break;
                    case RESTableMetaCondition.Safepost:
                        mc.SafePost = value;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            });

            make(renames);
            make(regular);

            mc.Processors = new IProcessor[] {mc.Add, mc.Rename, mc.Select}.Where(p => p != null).ToArray();
            mc.HasProcessors = mc.Processors.Any();
            mc.CanUseExternalCounter = mc.Search == null && mc.Distinct == null && mc.Limit.Number == -1 && mc.Offset.Number == 0;

            if (mc.OrderBy != null)
            {
                if (mc.Rename?.Any(p => p.Key.Key.EqualsNoCase(mc.OrderBy.Key)) == true
                    && !mc.Rename.Any(p => p.Value.EqualsNoCase(mc.OrderBy.Key)))
                    throw new InvalidSyntax(InvalidMetaConditionSyntax,
                        $"The {(mc.OrderBy is OrderByAscending ? RESTableMetaCondition.Order_asc : RESTableMetaCondition.Order_desc)} " +
                        "meta-condition cannot refer to a property x that is to be renamed " +
                        "unless some other property is renamed to x");
            }

            if (mc.Select != null && mc.Rename != null)
            {
                if (mc.Select.Any(pc => mc.Rename.Any(p => p.Key.Key.EqualsNoCase(pc.Key)) &&
                                        !mc.Rename.Any(p => p.Value.EqualsNoCase(pc.Key))))
                    throw new InvalidSyntax(InvalidMetaConditionSyntax,
                        "A 'Select' meta-condition cannot refer to a property x that is " +
                        "to be renamed unless some other property is renamed to x. Use the " +
                        "new property name instead.");
            }

            return mc;
        }

        private static string GetTypeString(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "integer";
            if (type == typeof(bool)) return "boolean";
            return null;
        }

        /// <summary>
        /// Converts the MetaConditions object into a collection of IUriCondition instances
        /// </summary>
        public IEnumerable<IUriCondition> AsConditionList()
        {
            var list = new List<IUriCondition>();
            if (Unsafe)
                list.Add(new UriCondition(RESTableMetaCondition.Unsafe, "true"));
            if (Limit.Number > -1)
                list.Add(new UriCondition(RESTableMetaCondition.Limit, Limit.Number.ToString()));
            if (Offset.Number == int.MinValue)
                list.Add(new UriCondition(RESTableMetaCondition.Offset, "-∞"));
            else if (Offset.Number == int.MaxValue)
                list.Add(new UriCondition(RESTableMetaCondition.Offset, "∞"));
            else if (Offset.Number != 0)
                list.Add(new UriCondition(RESTableMetaCondition.Offset, Offset.Number.ToString()));
            if (OrderBy is OrderByDescending)
                list.Add(new UriCondition(RESTableMetaCondition.Order_desc, OrderBy.Term.ToString()));
            else if (OrderBy != null)
                list.Add(new UriCondition(RESTableMetaCondition.Order_asc, OrderBy.Term.ToString()));
            if (Select != null)
                list.Add(new UriCondition(RESTableMetaCondition.Select, string.Join(",", Select)));
            if (Add != null)
                list.Add(new UriCondition(RESTableMetaCondition.Add, string.Join(",", Add)));
            if (Rename != null)
                list.Add(new UriCondition(RESTableMetaCondition.Rename, string.Join(",", Rename.Select(r => $"{r.Key}->{r.Value}"))));
            if (Distinct != null)
                list.Add(new UriCondition(RESTableMetaCondition.Distinct, "true"));
            if (Search is RegexSearch)
                list.Add(new UriCondition(RESTableMetaCondition.Search_regex, Search.GetValueLiteral()));
            else if (Search != null)
                list.Add(new UriCondition(RESTableMetaCondition.Search, Search.GetValueLiteral()));
            if (SafePost != null)
                list.Add(new UriCondition(RESTableMetaCondition.Safepost, SafePost));
            return list;
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
            copy.Processors = new IProcessor[] {copy.Add, copy.Rename, copy.Select}.Where(p => p != null).ToArray();
            copy.HasProcessors = copy.Processors.Any();
            copy.CanUseExternalCounter = copy.Search == null && copy.Distinct == null && copy.Limit.Number == -1 && copy.Offset.Number == 0;
            return copy;
        }
    }
}