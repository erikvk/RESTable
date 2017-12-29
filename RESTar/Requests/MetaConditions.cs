using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Operators;

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
        public Limit Limit { get; internal set; } = Limit.NoLimit;

        /// <summary>
        /// An offset in the request's entities, on which enumeration will start when creating the 
        /// response
        /// </summary>
        public Offset Offset { get; internal set; } = Offset.NoOffset;

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
        /// The term to use for safepost
        /// </summary>
        internal string SafePost { get; set; }

        /// <summary>
        /// The format to use when serializing JSON
        /// </summary>
        internal Formatter Formatter { get; set; } = DbOutputFormat.Default;

        internal bool New { get; set; }
        internal bool Empty = true;
        internal bool Delete { get; set; }

        internal IProcessor[] Processors { get; private set; }
        internal bool HasProcessors { get; private set; }

        private static string AllMetaConditions =>
            $"{string.Join(", ", Enum.GetNames(typeof(RESTarMetaConditions)).Except(new[] {"New", "Delete"}))}";

        internal static MetaConditions Parse(List<UriCondition> uriMetaConditions, IResource resource,
            bool processors = true)
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
                var value = valueLiteral.ParseConditionValue();
                if (expectedType != value.GetType())
                    throw new InvalidSyntax(InvalidMetaConditionValueType,
                        $"Invalid data type assigned to meta-condition '{key}'. Expected {GetTypeString(expectedType)}.");
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
                        if (!processors) break;
                        mc.Select = new Select(resource, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Add:
                        if (!processors) break;
                        mc.Add = new Add(resource, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Rename:
                        if (!processors) break;
                        mc.Rename = new Rename(resource, (string) value, out dynamicDomain);
                        break;
                    case RESTarMetaConditions.Distinct:
                        if (!processors) break;
                        if ((bool) value)
                            mc.Distinct = new Distinct();
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

            if (processors)
            {
                mc.Processors = new IProcessor[] {mc.Add, mc.Rename, mc.Select, mc.Distinct}.Where(p => p != null).ToArray();
                mc.HasProcessors = mc.Processors.Any();
            }

            if (mc.OrderBy != null)
            {
                if (mc.Add?.Any(pc => pc.Key.EqualsNoCase(mc.OrderBy.Key)) == true)
                    mc.OrderBy.IsSqlQueryable = false;
                if (mc.Rename?.Any(pc => pc.Value.EqualsNoCase(mc.OrderBy.Key)) == true)
                    mc.OrderBy.IsSqlQueryable = false;
                if (mc.Rename?.Any(p => p.Key.Key.EqualsNoCase(mc.OrderBy.Key)) == true
                    && !mc.Rename.Any(p => p.Value.EqualsNoCase(mc.OrderBy.Key)))
                    throw new InvalidSyntax(InvalidMetaConditionSyntax,
                        $"The {(mc.OrderBy.Ascending ? "'Order_asc'" : "'Order_desc'")} " +
                        "meta-condition cannot refer to a property x that is to be renamed " +
                        "unless some other property is renamed to x");
                if (mc.OrderBy.Term.ScQueryable == false)
                    mc.OrderBy.IsSqlQueryable = false;
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
    }
}