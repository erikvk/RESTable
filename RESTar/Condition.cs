using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Dynamit;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    public sealed class Conditions : List<Condition>
    {
        public Condition this[string key]
        {
            get
            {
                return this.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        public dynamic this[string key, Operators op]
        {
            get
            {
                return this.FirstOrDefault(c =>
                    c.Operator.OpCode == op &&
                    string.Equals(c.Key, key, StringComparison.CurrentCultureIgnoreCase))?.Value;
            }
        }

        public IDictionary<string, dynamic> EqualsDict =>
            this.Where(c => c.Operator == Operators.EQUALS).ToDictionary(c => c.Key, c => c.Value);
    }

    public sealed class Condition
    {
        public string Key;
        public Operator Operator;
        public dynamic Value;

        internal static Conditions ParseConditions(IResource resource, string conditionString)
        {
            if (string.IsNullOrEmpty(conditionString))
                return null;
            return conditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid condition syntax");
                var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());

                Operator op;
                try
                {
                    op = Operator.Parse(matched);
                }
                catch
                {
                    throw new SyntaxException(
                        $"Invalid or missing operator(s) for condition '{s}'. The presence of one (and only one) " +
                        $"operator is required per condition. Make sure to URI encode all equals (\'=\' to \'%3D\') " +
                        $"and exclamation marks (\'!\' to \'%21\') in request URI value literals, to avoid capture. " +
                        $"Accepted operators: " + string.Join(", ", Operator.AvailableOperators));
                }
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                var dynamit = resource.TargetType.HasAttribute<DDictionaryAttribute>();

                string key;
                dynamic value;
                if (dynamit)
                {
                    var keyString = WebUtility.UrlDecode(pair[0]);
                    key = keyString;
                    var valueString = WebUtility.UrlDecode(pair[1]);
                    value = GetValue(valueString, dynamit: true);
                }
                else
                {
                    Type type;
                    var keyString = WebUtility.UrlDecode(pair[0]);
                    key = GetKey(resource, keyString, out type);
                    var valueString = WebUtility.UrlDecode(pair[1]);
                    value = GetValue(valueString, key);
                }

                return new Condition
                {
                    Key = key,
                    Operator = op,
                    Value = value
                };
            }).ToConditions();
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
                var containsOneAndOnlyOneEquals = s.Count(c => c == '=') == 1;
                if (!containsOneAndOnlyOneEquals)
                    throw new SyntaxException("Invalid operator for meta-condition. One and only one '=' is allowed");
                var pair = s.Split('=');

                RESTarMetaConditions metaCondition;
                var success = Enum.TryParse(pair[0].Capitalize(), out metaCondition);
                if (!success)
                    throw new SyntaxException($"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                                              $"{string.Join(", ", Enum.GetNames(typeof(RESTarMetaConditions)))}. For more info, see " +
                                              $"{Settings.Instance.HelpResourcePath}/topic=Meta-conditions");
                var expectedType = metaCondition.ExpectedType();
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

        private static dynamic GetValue(string valueString, string key = null, bool dynamit = false)
        {
            if (valueString == null)
                return null;
            if (valueString == "null")
                return null;
            if (valueString.First() == '\"')
                return dynamit ? valueString : valueString.Replace("\"", "");
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
            else if (DateTime.TryParseExact(valueString, "yyyy-MM-dd", null, DateTimeStyles.AssumeLocal, out dat) ||
                     DateTime.TryParseExact(valueString, "yyyy-MM-ddThh:mm:ss", null, DateTimeStyles.AssumeLocal,
                         out dat) ||
                     DateTime.TryParseExact(valueString, "O", null, DateTimeStyles.AssumeLocal, out dat))
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