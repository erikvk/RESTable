using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Operations;
using static System.StringComparison;
using static RESTar.Internal.ErrorCodes;

#pragma warning disable 612

namespace RESTar.Requests
{
    internal enum RESTarMetaConditions
    {
        Limit,
        Order_desc,
        Order_asc,
        Unsafe,
        Select,
        Add,
        Rename,
        [Obsolete] Dynamic,
        Safepost,
        New,
        Delete,
        Offset,
        Distinct
    }

    internal static class MetaConditionsExtensions
    {
        internal static Type ExpectedType(this RESTarMetaConditions condition)
        {
            switch (condition)
            {
                case RESTarMetaConditions.Limit: return typeof(int);
                case RESTarMetaConditions.Order_desc: return typeof(string);
                case RESTarMetaConditions.Order_asc: return typeof(string);
                case RESTarMetaConditions.Unsafe: return typeof(bool);
                case RESTarMetaConditions.Select: return typeof(string);
                case RESTarMetaConditions.Add: return typeof(string);
                case RESTarMetaConditions.Rename: return typeof(string);
                case RESTarMetaConditions.Dynamic: return typeof(bool);
                case RESTarMetaConditions.Safepost: return typeof(string);
                case RESTarMetaConditions.New: return typeof(bool);
                case RESTarMetaConditions.Delete: return typeof(bool);
                case RESTarMetaConditions.Offset: return typeof(int);
                case RESTarMetaConditions.Distinct: return typeof(bool);
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
        /// The limit by which the request's response body entity count should be restricted to
        /// </summary>
        public Limit Limit { get; internal set; } = Limit.NoLimit;

        /// <summary>
        /// An offset in the request's entities, on which enumeration will start when creating the 
        /// response
        /// </summary>
        public Offset Offset { get; internal set; } = Offset.NoOffset;

        /// <summary>
        /// Is this request unsafe?
        /// </summary>
        public bool Unsafe { get; set; }

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

        internal string SafePost { get; set; }
        internal bool New { get; set; }
        internal bool Empty = true;
        internal bool Delete { get; set; }
        internal IProcessor[] Processors { get; private set; }
        internal bool HasProcessors { get; private set; }

        internal static MetaConditions Parse(string metaConditionString, IResource resource,
            bool processors = true)
        {
            if (metaConditionString?.Equals("") != false)
                return null;
            metaConditionString = WebUtility.UrlDecode(metaConditionString);
            var mc = new MetaConditions {Empty = false};
            var mcStrings = metaConditionString.Split('&').ToList();
            var renameIndex = mcStrings.FindIndex(s => s.StartsWith("rename", CurrentCultureIgnoreCase));
            if (renameIndex != -1)
            {
                var rename = mcStrings[renameIndex];
                mcStrings.RemoveAt(renameIndex);
                mcStrings.Insert(0, rename);
            }
            ICollection<string> dynamicDomain = default;
            foreach (var s in mcStrings)
            {
                if (s == "")
                    throw new SyntaxException(InvalidMetaConditionSyntax, "Invalid meta-condition syntax");
                var containsOneAndOnlyOneEquals = s.Count(c => c == '=') == 1;
                if (!containsOneAndOnlyOneEquals)
                    throw new SyntaxException(InvalidMetaConditionOperator,
                        "Invalid operator for meta-condition. One and only one '=' is allowed");
                var pair = s.Split('=');
                if (!Enum.TryParse(pair[0], true, out RESTarMetaConditions metaCondition))
                    throw new SyntaxException(InvalidMetaConditionKey,
                        $"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                        $"{string.Join(", ", Enum.GetNames(typeof(RESTarMetaConditions)).Except(new[] {"New", "Delete"}))}. " +
                        $"For more info, see {Settings.Instance.HelpResourcePath}/topic=Meta-conditions");
                var expectedType = metaCondition.ExpectedType();
                var value = pair[1].ParseConditionValue();
                if (expectedType != value.GetType())
                    throw new SyntaxException(InvalidMetaConditionValueType,
                        $"Invalid data type assigned to meta-condition '{pair[0]}'. " +
                        $"Expected {GetTypeString(expectedType)}.");
                switch (metaCondition)
                {
                    case RESTarMetaConditions.Rename:
                        if (!processors) break;
                        mc.Rename = new Rename(resource, (string) value, out dynamicDomain);
                        break;
                    case RESTarMetaConditions.Limit:
                        mc.Limit = (Limit) (int) value;
                        break;
                    case RESTarMetaConditions.Order_desc:
                        mc.OrderBy = new OrderBy(resource, true, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Order_asc:
                        mc.OrderBy = new OrderBy(resource, false, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Unsafe:
                        mc.Unsafe = value;
                        break;
                    case RESTarMetaConditions.Select:
                        if (!processors) break;
                        mc.Select = new Select(resource, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Distinct:
                        if (!processors) break;
                        if ((bool) value)
                            mc.Distinct = new Distinct();
                        break;
                    case RESTarMetaConditions.Add:
                        if (!processors) break;
                        mc.Add = new Add(resource, (string) value, dynamicDomain);
                        break;
                    case RESTarMetaConditions.Dynamic: break;
                    case RESTarMetaConditions.Safepost:
                        mc.SafePost = value;
                        break;
                    case RESTarMetaConditions.New:
                        mc.New = value;
                        break;
                    case RESTarMetaConditions.Delete:
                        mc.Delete = value;
                        break;
                    case RESTarMetaConditions.Offset:
                        mc.Offset = (Offset) (int) value;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }

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
                        throw new SyntaxException(InvalidMetaConditionSyntax,
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
                        throw new SyntaxException(InvalidMetaConditionSyntax,
                            "A 'Select' meta-condition cannot refer to a property x that is " +
                            "to be renamed unless some other property is renamed to x");
                }
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