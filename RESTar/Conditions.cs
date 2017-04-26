using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using RESTar.Internal;
using static System.Net.WebUtility;
using static RESTar.ErrorCode;
using static RESTar.RESTarConfig;

namespace RESTar
{
    public sealed class Conditions : List<Condition>
    {
        internal IResource Resource;
        public bool AllStarcounterQueryable => this.All(c => c.IsStarcounterQueryable);
        public Conditions StarcounterQueryable => this.Where(c => c.IsStarcounterQueryable).ToConditions();
        public Conditions NonStarcounterQueryable => this.Where(c => !c.IsStarcounterQueryable).ToConditions();

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
                        string.Equals(c.Key, key, StringComparison.CurrentCultureIgnoreCase))
                    ?.Value;
            }
        }

        public void AdaptTo(Type type)
        {
            if (type == Resource.TargetType)
                return;
            var typeProperties = GetPropertyList(type);
            RemoveAll(cond => typeProperties.All(prop => cond.PropertyChain.FirstOrDefault()?.Name != prop.GetDataMemberNameOrName()));
            ForEach(condition => Migrate(type));
        }

        public IDictionary<string, dynamic> EqualsDict =>
            this.Where(c => c.Operator == Operators.EQUALS).ToDictionary(c => c.Key, c => c.Value);

        internal static Conditions Parse(string conditionString, IResource resource)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            return conditionString.Split('&')
                .Select(s =>
                {
                    if (s == "")
                        throw new SyntaxException("Invalid condition syntax", InvalidConditionSyntaxError);
                    var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                    Operator op;
                    if (!Operator.TryParse(matched, out op))
                        throw new OperatorException(s);
                    var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                    var keyString = UrlDecode(pair[0]);
                    var chain = PropertyChain.Parse(keyString, resource);
                    var valueString = UrlDecode(pair[1]);
                    var value = GetValue(valueString);
                    return new Condition(chain, op, value);
                })
                .ToConditions();
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

        internal ICollection<T> Evaluate<T>(IEnumerable<T> entities)
        {
            return entities.Where(entity => this.All(condition => condition.HoldsFor(entity))).ToList();
        }

        internal void Migrate(Type type)
        {
            if (type == Resource.TargetType) return;
            ForEach(condition => condition.Migrate(type));
        }

        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};
    }
}