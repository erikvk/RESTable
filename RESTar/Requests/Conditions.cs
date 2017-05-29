using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dynamit;
using RESTar.Internal;
using RESTar.Operations;
using static System.Globalization.DateTimeStyles;
using static RESTar.ErrorCode;

namespace RESTar.Requests
{
    public sealed class Conditions : List<Condition>, IFilter
    {
        internal Type Resource;
        internal Conditions StarcounterQueryable => this.Where(c => c.ScQueryable).ToConditions(Resource);
        internal Conditions NonStarcounterQueryable => this.Where(c => !c.ScQueryable).ToConditions(Resource);
        internal Conditions Equality => this.Where(c => c.Operator.Equality).ToConditions(Resource);
        internal Conditions Compare => this.Where(c => c.Operator.Compare).ToConditions(Resource);
        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};
        public Condition this[string key] => this.FirstOrDefault(c => c.Key.EqualsNoCase(key));

        public dynamic this[string key, Operators op] => this
            .FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key))
            ?.Value;

        internal Conditions(Type resource)
        {
            Resource = resource;
        }

        public static Conditions Parse(string conditionString, IResource resource)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            return conditionString.Split('&')
                .Select(s =>
                {
                    if (s == "")
                        throw new SyntaxException(InvalidConditionSyntaxError, "Invalid condition syntax");
                    s = s.ReplaceFirst("%3E=", ">=", out bool replaced);
                    if (!replaced) s = s.ReplaceFirst("%3C=", "<=", out replaced);
                    if (!replaced) s = s.ReplaceFirst("%3E", ">", out replaced);
                    if (!replaced) s = s.ReplaceFirst("%3C", "<", out replaced);
                    var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                    if (!Operator.TryParse(matched, out Operator op))
                        throw new OperatorException(s);
                    var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                    var keyString = WebUtility.UrlDecode(pair[0]);
                    var chain = PropertyChain.Parse(keyString, resource);
                    var valueString = WebUtility.UrlDecode(pair[1]);
                    var value = GetValue(valueString);
                    return new Condition(chain, op, value);
                })
                .ToConditions(resource.TargetType);
        }

        public static dynamic GetValue(string valueString)
        {
            if (valueString == null)
                return null;
            if (valueString == "null")
                return null;
            if (valueString.First() == '\"' && valueString.Last() == '\"')
                return valueString.Remove(0, 1).Remove(valueString.Length - 2, 1);
            dynamic obj;
            var dtStyle = Settings._LocalTimes ? AssumeLocal : AssumeUniversal;
            if (bool.TryParse(valueString, out bool boo))
                obj = boo;
            else if (int.TryParse(valueString, out int _int))
                obj = _int;
            else if (decimal.TryParse(valueString, out decimal dec))
                obj = decimal.Round(dec, 6);
            else if (DateTime.TryParseExact(valueString, "yyyy-MM-dd", null, dtStyle, out DateTime dat) ||
                     DateTime.TryParseExact(valueString, "yyyy-MM-ddTHH:mm:ss", null, dtStyle, out dat) ||
                     DateTime.TryParseExact(valueString, "O", null, dtStyle, out dat))
                obj = dat;
            else obj = valueString;
            return obj;
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            var type = typeof(T);
            if (type != Resource && Resource != typeof(DDictionary))
            {
                var newTypeProperties = type.GetStaticProperties();
                RemoveAll(cond => newTypeProperties.All(prop => prop.Name
                                                                != cond.PropertyChain.FirstOrDefault()?.Name));
                ForEach(condition => condition.Migrate(type));
                Resource = type;
            }
            return entities.Where(entity => this.All(condition => condition.HoldsFor(entity)));
        }

        public WhereClause ToWhereClause()
        {
            if (!this.Any()) return new WhereClause();
            var stringPart = new List<string>();
            var valuesPart = new List<object>();
            StarcounterQueryable.ForEach(c =>
            {
                if (c.Value == null)
                    stringPart.Add($"t.{c.PropertyChain.DbKey.Fnuttify()} " +
                                   $"{(c.Operator == Operators.NOT_EQUALS ? "IS NOT NULL" : "IS NULL")} ");
                else
                {
                    stringPart.Add($"t.{c.PropertyChain.DbKey.Fnuttify()} {c.Operator.SQL}?");
                    valuesPart.Add(c.Value);
                }
            });
            return new WhereClause
            {
                stringPart = $"WHERE {string.Join(" AND ", stringPart)}",
                valuesPart = valuesPart.ToArray()
            };
        }
    }
}