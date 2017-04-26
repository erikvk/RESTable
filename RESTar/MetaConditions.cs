using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Internal;

namespace RESTar
{
    public sealed class MetaConditions
    {
        internal int Limit { get; set; } = -1;
        internal OrderBy OrderBy { get; private set; }
        internal bool Unsafe { get; set; }
        internal List<PropertyChain> Select { get; private set; }
        internal List<PropertyChain> Add { get; private set; }
        internal IDictionary<PropertyChain, string> Rename { get; private set; }
        internal bool Dynamic { get; private set; }
        internal string SafePost { get; private set; }

        internal PostOperations PostOperation
        {
            get
            {
                if (Select == null && Rename == null && Add == null) return PostOperations.NoOperation;
                if (Select != null && Rename == null && Add == null) return PostOperations.Select;
                if (Select == null && Rename != null && Add == null) return PostOperations.Rename;
                if (Select != null && Rename != null && Add == null) return PostOperations.SelectRename;
                if (Select == null && Rename == null && Add != null) return PostOperations.Add;
                if (Select != null && Rename == null && Add != null) return PostOperations.SelectAdd;
                if (Select == null && Rename != null && Add != null) return PostOperations.RenameAdd;
                if (Select != null && Rename != null && Add != null) return PostOperations.SelectRenameAdd;
                return PostOperations.Error;
            }
        }

        internal static MetaConditions Parse(string metaConditionString, IResource resource)
        {
            if (metaConditionString?.Equals("") != false)
                return null;
            metaConditionString = WebUtility.UrlDecode(metaConditionString);
            var mc = new MetaConditions();

            foreach (var s in metaConditionString.Split('&'))
            {
                if (s == "")
                    throw new SyntaxException("Invalid meta-condition syntax",
                        ErrorCode.InvalidMetaConditionSyntaxError);

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
                                              $"Expected {Condition.GetTypeString(expectedType)}.",
                        ErrorCode.InvalidMetaConditionValueTypeError);

                switch (metaCondition)
                {
                    case RESTarMetaConditions.Limit:
                        mc.Limit = value;
                        break;
                    case RESTarMetaConditions.Order_desc:
                        mc.OrderBy = new OrderBy
                        {
                            Descending = true,
                            PropertyChain = PropertyChain.Parse((string) value, resource)
                        };
                        break;
                    case RESTarMetaConditions.Order_asc:
                        mc.OrderBy = new OrderBy
                        {
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
                            .ToList();
                        break;
                    case RESTarMetaConditions.Add:
                        mc.Add = ((string) value).Split(',')
                            .Select(str => PropertyChain.Parse(str, resource))
                            .ToList();
                        break;
                    case RESTarMetaConditions.Rename:
                        mc.Rename = ((string) value).Split(',')
                            .ToDictionary(
                                str => PropertyChain.Parse(
                                    str.Split(new[] {"->"}, StringSplitOptions.None)[0].ToLower(),
                                    resource),
                                str => str.Split(new[] {"->"}, StringSplitOptions.None)[1]
                            );
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
            }
            return mc;
        }
    }

    internal enum PostOperations
    {
        Error,
        NoOperation,
        Select,
        Rename,
        Add,
        SelectRename,
        SelectAdd,
        RenameAdd,
        SelectRenameAdd
    }
}