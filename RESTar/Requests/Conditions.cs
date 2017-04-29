using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using Dynamit;
using RESTar.Internal;
using RESTar.Operations;

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

        internal static Conditions Parse(string conditionString, IResource resource)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            return conditionString.Split('&')
                .Select(s =>
                {
                    if (s == "")
                        throw new SyntaxException("Invalid condition syntax", ErrorCode.InvalidConditionSyntaxError);
                    var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                    Operator op;
                    if (!Operator.TryParse(matched, out op))
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

        internal static dynamic GetValue(string valueString)
        {
            if (valueString == null)
                return null;
            if (valueString == "null")
                return null;
            if (valueString.First() == '\"' && valueString.Last() == '\"')
                return valueString.Remove(0, 1).Remove(valueString.Length - 2, 1);
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

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            var type = typeof(T);
            if (type != Resource && Resource != typeof(DDictionary))
            {
                var newTypeProperties = type.GetPropertyList();
                RemoveAll(cond => newTypeProperties.All(prop => prop.RESTarMemberName()
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