using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Starcounter;

namespace RESTar
{
    public sealed class Condition
    {
        public string Key;
        public Operator Operator;
        public object Value;

        internal static IList<Condition> ParseConditions(Type resource, string conditionString)
        {
            if (string.IsNullOrEmpty(conditionString))
                return null;

            return conditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid condition syntax");
                var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                var op = Operators.FirstOrDefault(o => o.Common == matched);
                if (op == null)
                {
                    throw new SyntaxException("Invalid or missing operator for condition. The presence of one " +
                                              "(and only one) operator is required per condition. Accepted operators: " +
                                              string.Join(", ", Operators.Select(o => o.Common)));
                }
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                return new Condition
                {
                    Key = GetKey(resource, pair[0]),
                    Operator = op,
                    Value = GetValue(pair[1])
                };
            }).ToList();
        }

        internal static IDictionary<string, object> ParseMetaConditions(string metConditionString)
        {
            if (metConditionString?.Equals("") != false)
                return null;

            return metConditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid meta-condition syntax");
                var op = Operators.FirstOrDefault(o => s.Contains(o.Common));
                if (op?.Common != "=")
                    throw new SyntaxException("Invalid operator for meta-condition. Only '=' is accepted");
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                Type typeCheck;
                var success = MetaConditions.TryGetValue(pair[0].ToLower(), out typeCheck);
                if (!success)
                    throw new SyntaxException($"Invalid meta-condition '{pair[0]}'. Available meta-conditions: " +
                                              $"{string.Join(", ", MetaConditions.Keys)}. For more info, see " +
                                              $"{Settings.Instance.HelpResourcePath}/topic=Meta-conditions");
                var value = GetValue(pair[1]);
                if (value.GetType() != typeCheck)
                    throw new SyntaxException($"Invalid data type assigned to meta-condition '{pair[0]}'. Expected " +
                                              $"{(typeCheck == typeof(decimal) ? "number" : typeCheck.FullName)}");
                return new KeyValuePair<string, object>(pair[0].ToLower(), value);
            }).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static readonly IDictionary<string, Type> MetaConditions = new Dictionary<string, Type>
        {
            ["limit"] = typeof(decimal),
            ["order_desc"] = typeof(string),
            ["order_asc"] = typeof(string),
            ["unsafe"] = typeof(bool),
            ["select"] = typeof(string),
            ["rename"] = typeof(string),
            ["dynamic"] = typeof(bool)
        };

        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};

        private static readonly IEnumerable<Operator> Operators = new List<Operator>
        {
            new Operator("=", "="),
            new Operator("!=", "<>"),
            new Operator("<", "<"),
            new Operator(">", ">"),
            new Operator(">=", ">="),
            new Operator("<=", "<=")
        };

        private static string GetKey(Type resource, string keyString)
        {
            var columns = resource.GetColumns();
            if (!keyString.Contains('.'))
                return columns.FindColumn(resource, keyString).Name;

            keyString = keyString.ToLower();
            var parts = keyString.Split('.');
            if (parts.Length == 1)
                throw new SyntaxException($"Invalid condition '{keyString}'");
            var types = new List<Type>();
            foreach (var str in parts.Take(parts.Length - 1))
            {
                var containingType = types.LastOrDefault() ?? resource;
                var type = containingType
                    .GetProperties()
                    .Where(prop => str == prop.Name.ToLower())
                    .Select(prop => prop.PropertyType)
                    .FirstOrDefault();

                if (type == null)
                    throw new UnknownColumnException(resource, keyString);

                if (type.GetAttribute<RESTarAttribute>()?.AvailableMethods.Contains(RESTarMethods.GET) != true)
                    throw new SyntaxException($"RESTar does not have read access to resource '{type.FullName}' " +
                                              $"referenced in '{keyString}'.");

                if (!type.HasAttribute<DatabaseAttribute>())
                    throw new SyntaxException($"A part '{str}' of condition key '{keyString}' referenced type " +
                                              $"'{type.FullName}', which is of a non-database type. Only references " +
                                              "to database types (resources) can be used in queries.");
                types.Add(type);
            }
            var lastType = types.Last();
            var lastColumns = lastType.GetColumns();
            var lastColumn = lastColumns.FindColumn(lastType, parts.Last());
            parts[parts.Length - 1] = lastColumn.Name;
            return string.Join(".", parts);
        }

        private static object GetValue(string valueString)
        {
            valueString = HttpUtility.UrlDecode(valueString);
            if (valueString == null)
                return null;
            if (valueString == "null")
                return null;
            if (valueString.First() == '\"')
                return valueString.Replace("\"", "");
            object obj;
            decimal dec;
            bool boo;
            DateTime dat;
            if (bool.TryParse(valueString, out boo))
                obj = boo;
            else if (decimal.TryParse(valueString, out dec))
                obj = dec;
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