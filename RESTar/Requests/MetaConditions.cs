using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Operations;
using static System.StringComparison;
using static RESTar.Internal.ErrorCodes;

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
        Dynamic,
        Safepost,
        New,
        Delete
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
                default: throw new ArgumentOutOfRangeException(nameof(condition), condition, null);
            }
        }
    }

    public sealed class MetaConditions
    {
        internal Limit Limit { get; set; } = Limit.NoLimit;
        internal OrderBy OrderBy { get; set; }
        internal bool Unsafe { get; set; }
        internal Select Select { get; set; }
        internal Add Add { get; set; }
        internal Rename Rename { get; set; }
        internal bool Dynamic { get; set; }
        internal string SafePost { get; set; }
        internal bool New { get; set; }
        internal bool Empty = true;
        internal bool Delete { get; set; }

        internal static MetaConditions Parse(string metaConditionString, IResource resource)
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
            var dynamicMembers = new List<string>();

            foreach (var s in mcStrings)
            {
                if (s == "")
                    throw new SyntaxException(InvalidMetaConditionSyntaxError, "Invalid meta-condition syntax");

                var containsOneAndOnlyOneEquals = s.Count(c => c == '=') == 1;
                if (!containsOneAndOnlyOneEquals)
                    throw new SyntaxException(InvalidMetaConditionOperatorError,
                        "Invalid operator for meta-condition. One and only one '=' is allowed");
                var pair = s.Split('=');

                if (!Enum.TryParse(pair[0], true, out RESTarMetaConditions metaCondition))
                    throw new SyntaxException(InvalidMetaConditionKey,
                        $"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                        $"{string.Join(", ", Enum.GetNames(typeof(RESTarMetaConditions)).Except(new[] {"New", "Delete"}))}. " +
                        $"For more info, see {Settings.Instance.HelpResourcePath}/topic=Meta-conditions");

                var expectedType = metaCondition.ExpectedType();
                var value = Conditions.GetValue(pair[1]);
                if (expectedType != value.GetType())
                    throw new SyntaxException(InvalidMetaConditionValueTypeError,
                        $"Invalid data type assigned to meta-condition '{pair[0]}'. " +
                        $"Expected {GetTypeString(expectedType)}.");

                switch (metaCondition)
                {
                    case RESTarMetaConditions.Limit:
                        mc.Limit = (int) value;
                        break;
                    case RESTarMetaConditions.Order_desc:
                        mc.OrderBy = new OrderBy(resource, true, (string) value, dynamicMembers);
                        break;
                    case RESTarMetaConditions.Order_asc:
                        mc.OrderBy = new OrderBy(resource, false, (string) value, dynamicMembers);
                        break;
                    case RESTarMetaConditions.Unsafe:
                        mc.Unsafe = value;
                        break;
                    case RESTarMetaConditions.Select:
                        mc.Select = ((string) value).Split(',')
                            .Select(str => PropertyChain.Parse(str, resource, resource.IsDynamic, dynamicMembers))
                            .ToSelect();
                        break;
                    case RESTarMetaConditions.Add:
                        mc.Add = ((string) value).Split(',')
                            .Select(str => PropertyChain.Parse(str, resource, resource.IsDynamic))
                            .ToAdd();
                        break;
                    case RESTarMetaConditions.Rename:
                        mc.Rename = Rename.Parse((string) value, resource);
                        dynamicMembers.AddRange(mc.Rename.Values);
                        break;
                    case RESTarMetaConditions.Dynamic:
                        mc.Dynamic = value;
                        break;
                    case RESTarMetaConditions.Safepost:
                        mc.SafePost = value;
                        break;
                    case RESTarMetaConditions.New:
                        mc.New = value;
                        break;
                    case RESTarMetaConditions.Delete:
                        mc.Delete = value;
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }

                if (mc.OrderBy != null)
                {
                    if (mc.Add?.Any(pc => pc.Key.EqualsNoCase(mc.OrderBy.Key)) == true)
                        mc.OrderBy.IsStarcounterQueryable = false;
                    if (mc.Rename?.Any(pc => pc.Value.EqualsNoCase(mc.OrderBy.Key)) == true)
                        mc.OrderBy.IsStarcounterQueryable = false;
                    if (mc.Rename?.Any(p => p.Key.Key.EqualsNoCase(mc.OrderBy.Key)) == true
                        && !mc.Rename.Any(p => p.Value.EqualsNoCase(mc.OrderBy.Key)))
                        throw new SyntaxException(InvalidMetaConditionSyntaxError,
                            $"The {(mc.OrderBy.Ascending ? "'Order_asc'" : "'Order_desc'")} " +
                            "meta-condition cannot refer to a property x that is to be renamed " +
                            "unless some other property is renamed to x");
                    if (mc.OrderBy.PropertyChain.ScQueryable == false)
                        mc.OrderBy.IsStarcounterQueryable = false;
                }
                if (mc.Select != null && mc.Rename != null)
                {
                    if (mc.Select.Any(pc => mc.Rename.Any(p => p.Key.Key.EqualsNoCase(pc.Key)) &&
                                            !mc.Rename.Any(p => p.Value.EqualsNoCase(pc.Key))))
                        throw new SyntaxException(InvalidMetaConditionSyntaxError,
                            "A 'Select' meta-condition cannot refer to a property x that is " +
                            "to be renamed unless some other property is renamed to x");
                }
            }
            return mc;
        }

        internal bool HasProcessors => Select != null || Rename != null || Add != null;

        internal void DeactivateProcessors()
        {
            Add = null;
            Select = null;
            Rename = null;
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