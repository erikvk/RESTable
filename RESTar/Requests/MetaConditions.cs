using System;
using System.Linq;
using System.Net;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar.Requests
{
    public sealed class MetaConditions
    {
        internal Limit Limit { get; set; } = Limit.NoLimit;
        internal OrderBy OrderBy { get; private set; }
        internal bool Unsafe { get; set; }
        internal Select Select { get; private set; }
        internal Add Add { get; private set; }
        internal Rename Rename { get; private set; }
        internal bool Dynamic { get; private set; }
        internal string SafePost { get; private set; }

        internal static MetaConditions Parse(string metaConditionString, IResource resource)
        {
            if (metaConditionString?.Equals("") != false)
                return null;
            metaConditionString = WebUtility.UrlDecode(metaConditionString);
            var mc = new MetaConditions();

            foreach (var s in metaConditionString.Split('&'))
            {
                if (s == "")
                    throw new SyntaxException("Invalid meta-condition syntax", ErrorCode.InvalidMetaConditionSyntaxError);

                var containsOneAndOnlyOneEquals = s.Count(c => c == '=') == 1;
                if (!containsOneAndOnlyOneEquals)
                    throw new SyntaxException("Invalid operator for meta-condition. One and only one '=' is allowed",
                        ErrorCode.InvalidMetaConditionOperatorError);
                var pair = s.Split('=');

                RESTarMetaConditions metaCondition;
                if (!Enum.TryParse(pair[0], true, out metaCondition))
                    throw new SyntaxException($"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                                              $"{string.Join(", ", Enum.GetNames(typeof(RESTarMetaConditions)))}. For more info, see " +
                                              $"{Settings.Instance.HelpResourcePath}/topic=Meta-conditions",
                        ErrorCode.InvalidMetaConditionKey);

                var expectedType = metaCondition.ExpectedType();
                var value = Conditions.GetValue(pair[1]);
                if (expectedType != value.GetType())
                    throw new SyntaxException($"Invalid data type assigned to meta-condition '{pair[0]}'. " +
                                              $"Expected {GetTypeString(expectedType)}.",
                        ErrorCode.InvalidMetaConditionValueTypeError);

                switch (metaCondition)
                {
                    case RESTarMetaConditions.Limit:
                        mc.Limit = (int) value;
                        break;
                    case RESTarMetaConditions.Order_desc:
                        mc.OrderBy = new OrderBy
                        {
                            Resource = resource,
                            Descending = true,
                            PropertyChain = PropertyChain.Parse((string) value, resource)
                        };
                        break;
                    case RESTarMetaConditions.Order_asc:
                        mc.OrderBy = new OrderBy
                        {
                            Resource = resource,
                            Descending = false,
                            PropertyChain = PropertyChain.Parse((string) value, resource)
                        };
                        break;
                    case RESTarMetaConditions.Unsafe:
                        mc.Unsafe = value;
                        break;
                    case RESTarMetaConditions.Select:
                        mc.Select = ((string) value).Split(',')
                            .Select(str => PropertyChain.Parse(str, resource))
                            .ToSelect();
                        break;
                    case RESTarMetaConditions.Add:
                        mc.Add = ((string) value).Split(',')
                            .Select(str => PropertyChain.Parse(str, resource))
                            .ToAdd();
                        break;
                    case RESTarMetaConditions.Rename:
                        mc.Rename = Rename.Parse((string) value, resource);
                        break;
                    case RESTarMetaConditions.Dynamic:
                        mc.Dynamic = value;
                        break;
                    case RESTarMetaConditions.Safepost:
                        mc.SafePost = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (mc.OrderBy != null)
                {
                    if (mc.Add?.Any(pc => pc.Key.EqualsNoCase(mc.OrderBy.Key)) == true)
                        mc.OrderBy.IsStarcounterQueryable = false;
                    if (mc.Rename?.Any(pc => pc.Value.EqualsNoCase(mc.OrderBy.Key)) == true)
                        mc.OrderBy.IsStarcounterQueryable = false;
                    if (mc.Rename?.Any(pc => pc.Key.Key.EqualsNoCase(mc.OrderBy.Key)) == true)
                        throw new SyntaxException($"The {(mc.OrderBy.Ascending ? "'Order_asc'" : "'Order_desc'")} " +
                                                  "meta-condition cannot refer to a property that is to be renamed",
                            ErrorCode.InvalidMetaConditionSyntaxError);
                    if (mc.OrderBy.PropertyChain.ScQueryable == false)
                        mc.OrderBy.IsStarcounterQueryable = false;
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