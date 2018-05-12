using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Requests.Filters;
using RESTar.Requests.Processors;
using RESTar.Results;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Requests.Operators;

#pragma warning disable 612

namespace RESTar.Requests
{
    internal enum RESTarMetaConditions
    {
        Unsafe,
        Limit,
        Offset,
        Order_asc,
        Order_desc,
        Select,
        Add,
        Rename,
        Distinct,
        Search,
        Safepost,
        Format,
        New,
        Delete
    }

    internal static class MetaConditionsExtensions
    {
        internal static Type GetExpectedType(this RESTarMetaConditions condition)
        {
            switch (condition)
            {
                case RESTarMetaConditions.Unsafe: return typeof(bool);
                case RESTarMetaConditions.Limit: return typeof(int);
                case RESTarMetaConditions.Offset: return typeof(int);
                case RESTarMetaConditions.Order_asc: return typeof(string);
                case RESTarMetaConditions.Order_desc: return typeof(string);
                case RESTarMetaConditions.Select: return typeof(string);
                case RESTarMetaConditions.Add: return typeof(string);
                case RESTarMetaConditions.Rename: return typeof(string);
                case RESTarMetaConditions.Distinct: return typeof(bool);
                case RESTarMetaConditions.Search: return typeof(string);
                case RESTarMetaConditions.Safepost: return typeof(string);
                case RESTarMetaConditions.Format: return typeof(string);
                case RESTarMetaConditions.New: return typeof(bool);
                case RESTarMetaConditions.Delete: return typeof(bool);
                default: throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }
        }
    }

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

        /// <summary>
        /// The format to use when serializing JSON
        /// </summary>
        internal Formatter? Formatter { get; set; }

        internal bool New { get; set; }
        internal bool Empty = true;
        internal bool Delete { get; set; }

        internal IProcessor[] Processors { get; private set; }
        internal bool HasProcessors { get; private set; }
        internal bool CanUseExternalCounter { get; private set; } = true;

        private static string AllMetaConditions =>
            $"{string.Join(", ", Enum.GetNames(typeof(RESTarMetaConditions)).Except(new[] {"New", "Delete"}))}";

        internal static MetaConditions Parse(List<UriCondition> uriMetaConditions, IEntityResource resource)
        {
            if (!uriMetaConditions.Any()) return null;
            var renames = uriMetaConditions.Where(c => c.Key.EqualsNoCase("rename"));
            var regular = uriMetaConditions.Where(c => !c.Key.EqualsNoCase("rename"));
            var mc = new MetaConditions {Empty = false};
            ICollection<string> dynamicDomain = default;

            void make(IEnumerable<UriCondition> conds) => conds.ForEach(cond =>
            {
                var (key, op, valueLiteral) = (cond.Key, cond.Operator, cond.ValueLiteral);
                if (op.OpCode != EQUALS)
                    throw new InvalidSyntax(InvalidMetaConditionOperator,
                        "Invalid operator for meta-condition. One and only one '=' is allowed");
                if (!Enum.TryParse(key, true, out RESTarMetaConditions metaCondition))
                    throw new InvalidSyntax(InvalidMetaConditionKey,
                        $"Invalid meta-condition '{key}'. Available meta-conditions: {AllMetaConditions}");

                var expectedType = metaCondition.GetExpectedType();
                dynamic value;
                try
                {
                    value = Convert.ChangeType(valueLiteral, expectedType) ?? throw new Exception();
                }
                catch
                {
                    throw new InvalidSyntax(InvalidMetaConditionValueType,
                        $"Invalid data type assigned to meta-condition '{key}'. Expected {GetTypeString(expectedType)}.");
                }
                switch (metaCondition)
                {
                    case RESTarMetaConditions.Unsafe:
                        mc.Unsafe = value;
                        break;
                    case RESTarMetaConditions.Limit:
                        mc.Limit = (Limit) (int) value;
                        break;
                    case RESTarMetaConditions.Offset:
                        mc.Offset = (Offset) (int) value;
                        break;
                    case RESTarMetaConditions.Order_asc:
                        mc.OrderBy = new OrderBy(resource, false, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Order_desc:
                        mc.OrderBy = new OrderBy(resource, true, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Select:
                        mc.Select = new Select(resource, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Add:
                        mc.Add = new Add(resource, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Rename:
                        mc.Rename = new Rename(resource, (string) value, out dynamicDomain);
                        break;
                    case RESTarMetaConditions.Distinct:
                        if ((bool) value)
                            mc.Distinct = new Distinct();
                        break;
                    case RESTarMetaConditions.Search:
                        mc.Search = new Search((string) value);
                        break;
                    case RESTarMetaConditions.Safepost:
                        mc.SafePost = value;
                        break;
                    case RESTarMetaConditions.Format:
                        var formatName = (string) value;
                        var format = DbOutputFormat.GetByName(formatName) ?? throw new InvalidSyntax(UnknownFormatter,
                                         $"Could not find any output format by '{formatName}'. See RESTar.Admin.OutputFormat " +
                                         "for available output formats");
                        mc.Formatter = format.Format;
                        break;
                    case RESTarMetaConditions.New:
                        mc.New = value;
                        break;
                    case RESTarMetaConditions.Delete:
                        mc.Delete = value;
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
                        $"The {(mc.OrderBy.Ascending ? "'Order_asc'" : "'Order_desc'")} " +
                        "meta-condition cannot refer to a property x that is to be renamed " +
                        "unless some other property is renamed to x");
            }

            if (mc.Select != null && mc.Rename != null)
            {
                if (mc.Select.Any(pc => mc.Rename.Any(p => p.Key.Key.EqualsNoCase(pc.Key)) &&
                                        !mc.Rename.Any(p => p.Value.EqualsNoCase(pc.Key))))
                    throw new InvalidSyntax(InvalidMetaConditionSyntax,
                        "A 'Select' meta-condition cannot refer to a property x that is " +
                        "to be renamed unless some other property is renamed to x");
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
                list.Add(new UriCondition("unsafe", EQUALS, "true"));
            if (Limit.Number > -1)
                list.Add(new UriCondition("limit", EQUALS, Limit.Number.ToString()));
            if (Offset.Number > 0)
                list.Add(new UriCondition("offset", EQUALS, Offset.Number.ToString()));
            if (OrderBy != null)
            {
                var key = OrderBy.Descending ? "order_desc" : "order_asc";
                list.Add(new UriCondition(key, EQUALS, OrderBy.Term.ToString()));
            }
            if (Select != null)
                list.Add(new UriCondition("select", EQUALS, string.Join(",", Select)));
            if (Add != null)
                list.Add(new UriCondition("add", EQUALS, string.Join(",", Add)));
            if (Rename != null)
                list.Add(new UriCondition("rename", EQUALS, string.Join(",", Rename.Select(r => $"{r.Key}->{r.Value}"))));
            if (Distinct != null)
                list.Add(new UriCondition("distinct", EQUALS, "true"));
            if (Search != null)
                list.Add(new UriCondition("search", EQUALS, Search.Pattern));
            if (SafePost != null)
                list.Add(new UriCondition("safepost", EQUALS, SafePost));
            if (Formatter.HasValue)
                list.Add(new UriCondition("formatter", EQUALS, Formatter.Value.Name));
            return list;
        }
    }
}