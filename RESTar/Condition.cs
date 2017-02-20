using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dynamit;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public sealed class Condition
    {
        public string Key;
        public Operator Operator;
        public dynamic Value;

        internal static IList<Condition> ParseConditions(IResource resource, string conditionString)
        {
            if (string.IsNullOrEmpty(conditionString))
                return null;
            conditionString = WebUtility.UrlDecode(conditionString);
            return conditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid condition syntax");
                var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());

                Operator op;
                try
                {
                    op = Operators.First(o => o.Common == matched);
                }
                catch
                {
                    throw new SyntaxException("Invalid or missing operator for condition. The presence of one " +
                                              "(and only one) operator is required per condition. Accepted operators: " +
                                              string.Join(", ", Operators.Select(o => o.Common)));
                }
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                var dynamit = resource.TargetType.HasAttribute<DDictionaryAttribute>();

                string key;
                dynamic value;
                if (dynamit)
                {
                    key = pair[0];
                    value = GetValue(pair[1]);
                }
                else
                {
                    Type type;
                    key = GetKey(resource, pair[0], out type);
                    value = GetValue(pair[1], key);
                }

                return new Condition
                {
                    Key = key,
                    Operator = op,
                    Value = value
                };
            }).ToList();
        }

        internal static IDictionary<string, object> ParseMetaConditions(string metaConditionString)
        {
            if (metaConditionString?.Equals("") != false)
                return null;
            metaConditionString = WebUtility.UrlDecode(metaConditionString);
            return metaConditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid meta-condition syntax");
                var op = Operators.FirstOrDefault(o => s.Contains(o.Common));
                if (op?.Common != "=")
                    throw new SyntaxException("Invalid operator for meta-condition. Only '=' is accepted");
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);

                RESTarMetaConditions metaCondition;
                var success = Enum.TryParse(pair[0].Capitalize(), out metaCondition);
                if (!success)
                    throw new SyntaxException($"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                                              $"{string.Join(", ", RESTarConfig.MetaConditions.Keys)}. For more info, see " +
                                              $"{Settings.Instance.HelpResourcePath}/topic=Meta-conditions");
                var expectedType = RESTarConfig.MetaConditions[metaCondition];
                var value = GetValue(pair[1]);
                if (expectedType != value.GetType())
                    throw new SyntaxException($"Invalid data type assigned to meta-condition '{pair[0]}'. " +
                                              $"Expected {GetTypeString(expectedType)}.");
                return new KeyValuePair<string, object>(pair[0].ToLower(), value);
            }).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static string GetTypeString(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "integer";
            if (type == typeof(bool)) return "boolean";
            return null;
        }

        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};

        internal static readonly IEnumerable<Operator> Operators = new List<Operator>
        {
            new Operator("=", "="),
            new Operator("!=", "<>"),
            new Operator("<", "<"),
            new Operator(">", ">"),
            new Operator(">=", ">="),
            new Operator("<=", "<=")
        };

        private static string GetKey(IResource resource, string keyString, out Type keyType)
        {
            keyType = default(Type);
            keyString = keyString.ToLower();
            var columns = resource.TargetType.GetColumns();
            if (!keyString.Contains('.'))
            {
                if (keyString == "objectno")
                    return "ObjectNo";
                if (keyString == "objectid")
                    return "ObjectId";
                var column = columns.FindColumn(resource.TargetType, keyString);
                keyType = column.PropertyType;
                return column.Name;
            }
            var parts = keyString.Split('.');
            if (parts.Length == 1)
                throw new SyntaxException($"Invalid condition '{keyString}'");
            var types = new List<Type>();
            foreach (var str in parts.Take(parts.Length - 1))
            {
                var containingType = types.LastOrDefault() ?? resource.TargetType;
                var type = containingType
                    .GetProperties()
                    .Where(prop => str == prop.Name.ToLower())
                    .Select(prop => prop.PropertyType)
                    .FirstOrDefault();

                if (type == null)
                    throw new UnknownColumnException(resource.TargetType, keyString);

//                if (type.GetAttribute<RESTarAttribute>()?.AvailableMethods.Contains(RESTarMethods.GET) != true)
//                    throw new SyntaxException($"RESTar does not have read access to resource '{type.FullName}' " +
//                                              $"referenced in '{keyString}'.");
//
//                if (!type.HasAttribute<DatabaseAttribute>())
//                    throw new SyntaxException($"Part '{str}' in condition key '{keyString}' referenced a column of " +
//                                              $"type '{type.FullName}', which is of a non-resource type. Non-resource " +
//                                              "columns can only appear last in condition keys containing dot notation.");
                types.Add(type);
            }

            if (parts.Last() == "objectno" || parts.Last() == "objectid")
                return string.Join(".", parts);
            var lastType = types.Last();
            var lastColumns = lastType.GetColumns();
            var lastColumn = lastColumns.FindColumn(lastType, parts.Last());
            parts[parts.Length - 1] = lastColumn.Name;
            keyType = lastColumn.PropertyType;
            return string.Join(".", parts);
        }

        private static dynamic GetValue(string valueString, string key = null)
        {
            if (valueString == null)
                return null;
            if (valueString == "null")
                return null;
            if (valueString.First() == '\"')
                return valueString.Replace("\"", "");
            dynamic obj;
            int _int;
            decimal dec;
            bool boo;
            DateTime dat;
            if (bool.TryParse(valueString, out boo))
                obj = boo;
            else if (int.TryParse(valueString, out _int))
                obj = _int;
            else if (decimal.TryParse(valueString, out dec))
            {
                var rounded = decimal.Round(dec, 6);
                obj = rounded;
            }
            else if (DateTime.TryParse(valueString, out dat))
                obj = dat;
            else obj = valueString;
            return obj;
        }

        public override string ToString()
        {
            return Key + Operator + Value;
        }
    }
}