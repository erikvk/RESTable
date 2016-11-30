using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RESTar
{
    internal class Command
    {
        public Type Resource;
        public IList<Condition> Conditions;
        public IDictionary<string, object> MetaConditions;
        public readonly string Query;
        public bool Unsafe;
        public int Limit = -1;
        public OrderBy OrderBy;
        public readonly string Json;
        public string[] Select;

        internal Command(string query, string json)
        {
            if (query == null)
                throw new RESTarInternalException("Query not loaded");
            Query = query;
            Json = json?.RemoveTabsAndBreaks();

            var args = Query.Split('/');
            var argLength = args.Length;

            if (argLength == 1)
                return;

            if (args[1] == "")
                Resource = typeof(Resource);
            else Resource = args[1].FindResource();
            if (argLength == 2) return;

            Conditions = Condition.Parse(args[2]);
            if (Conditions != null &&
                (Resource == typeof(Resource) || Resource.IsSubclassOf(typeof(Resource))))
            {
                var nameCondition = Conditions.FirstOrDefault(c => c.Key == "name");
                if (nameCondition != null)
                    nameCondition.Value = nameCondition.Value.ToString().FindResource().FullName;
            }
            if (argLength == 3) return;

            MetaConditions = Condition.ParseMeta(args[3]);
            if (MetaConditions == null) return;

            if (MetaConditions.ContainsKey("limit"))
                Limit = decimal.ToInt32((decimal) MetaConditions["limit"]);
            if (MetaConditions.ContainsKey("unsafe"))
                Unsafe = (bool) MetaConditions["unsafe"];
            if (MetaConditions.ContainsKey("select"))
                Select = ((string) MetaConditions["select"]).Split(',').Select(s => s.ToLower()).ToArray();
            var orderKey = MetaConditions.Keys.FirstOrDefault(key => key.Contains("order"));
            if (orderKey == null) return;
            OrderBy = new OrderBy
            {
                Descending = orderKey.Contains("desc"),
                Key = MetaConditions[orderKey].ToString()
            };
        }

        internal List<object> GetExtension(bool? unsafeOverride = null)
        {
            if (unsafeOverride != null)
                Unsafe = unsafeOverride.Value;
            if (Unsafe)
                return Common.GetFromDb(Resource, Select, Conditions.ToWhereClause(), Limit, OrderBy).ToList();
            var items = Common.GetFromDb(Resource, Select, Conditions.ToWhereClause(), 2, OrderBy).ToList();
            if (items.Count > 1) throw new AmbiguousMatchException(Resource);
            return items;
        }
    }

    internal class Condition
    {
        public string Key;
        public Operator Operator;
        public object Value;

        public static IList<Condition> Parse(string conditionString)
        {
            if (conditionString?.Equals("") != false)
                return null;

            return conditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException("Invalid condition syntax");
                var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                var op = Operators.FirstOrDefault(o => o.Common == matched);
                if (op == null)
                    throw new SyntaxException("Invalid or missing operator for condition. The presence of one " +
                                              "(and only one) operator is required per condition. Accepted operators: " +
                                              string.Join(", ", Operators.Select(o => o.Common)));
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                return new Condition
                {
                    Key = pair[0].ToLower(),
                    Operator = op,
                    Value = GetValue(pair[1])
                };
            }).ToList();
        }

        public static IDictionary<string, object> ParseMeta(string conditionString)
        {
            if (conditionString?.Equals("") != false)
                return null;

            return conditionString.Split('&').Select(s =>
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
            ["select"] = typeof(string)
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

        private static object GetValue(string valueString)
        {
            valueString = HttpUtility.UrlDecode(valueString);
            if (valueString == null)
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

    internal class Operator
    {
        public readonly string Common;
        public readonly string SQL;

        public Operator(string common, string sql)
        {
            Common = common;
            SQL = sql;
        }

        public override string ToString()
        {
            return Common;
        }
    }
}