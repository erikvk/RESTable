using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar
{
    public sealed class Conditions : List<Condition>, IFilter
    {
        internal IResource Resource;
        internal Conditions SQL => this.Where(c => c.ScQueryable).ToConditions(Resource);
        internal Conditions PostSQL => this.Where(c => !c.ScQueryable || c.IsOfType<string>()).ToConditions(Resource);
        internal Conditions Equality => this.Where(c => c.Operator.Equality).ToConditions(Resource);
        internal Conditions Compare => this.Where(c => c.Operator.Compare).ToConditions(Resource);
        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};
        internal Conditions(IResource resource) => Resource = resource;

        public Condition this[string key]
        {
            get => this.FirstOrDefault(c => c.Key.EqualsNoCase(key));
            set => this.Add(value);
        }

        public dynamic this[string key, Operator op]
        {
            get => this
                .FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key))
                ?.Value;
            set => this[key] = new Condition
            (
                propertyChain: PropertyChain.Parse(key, Resource, Resource.DynamicConditionsAllowed),
                op: op,
                value: value
            );
        }

        public static Conditions Parse(string conditionString, IResource resource)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            var conditions = new Conditions(resource);
            conditionString.Split('&').ForEach(s =>
            {
                if (s == "")
                    throw new SyntaxException(ErrorCodes.InvalidConditionSyntaxError, "Invalid condition syntax");
                s = s.ReplaceFirst("%3E=", ">=", out bool replaced);
                if (!replaced) s = s.ReplaceFirst("%3C=", "<=", out replaced);
                if (!replaced) s = s.ReplaceFirst("%3E", ">", out replaced);
                if (!replaced) s = s.ReplaceFirst("%3C", "<", out replaced);
                var matched = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                if (!Operator.TryParse(matched, out Operator op))
                    throw new OperatorException(s);
                var pair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                var keyString = WebUtility.UrlDecode(pair[0]);
                var chain = PropertyChain.Parse(keyString, resource, resource.DynamicConditionsAllowed);
                var valueString = WebUtility.UrlDecode(pair[1]);
                var value = GetValue(valueString);
                if (chain.IsStatic && chain.LastOrDefault() is StaticProperty prop && prop.Type.IsEnum &&
                    value is string)
                {
                    try
                    {
                        value = Enum.Parse(prop.Type, value);
                    }
                    catch
                    {
                        throw new SyntaxException(ErrorCodes.InvalidConditionSyntaxError,
                            $"Invalid string value for condition '{chain.Key}'. The property type for '{prop.Name}' " +
                            $"has a predefined set of allowed values, not containing '{value}'.");
                    }
                }
                conditions.Add(new Condition(chain, op, value));
            });
            return conditions;
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
            if (bool.TryParse(valueString, out bool boo))
                obj = boo;
            else if (int.TryParse(valueString, out int _int))
                obj = _int;
            else if (decimal.TryParse(valueString, out decimal dec))
                obj = decimal.Round(dec, 6);
            else if (DateTime.TryParseExact(valueString, "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal,
                         out DateTime dat) ||
                     DateTime.TryParseExact(valueString, "yyyy-MM-ddTHH:mm:ss", null, DateTimeStyles.AssumeUniversal,
                         out dat) ||
                     DateTime.TryParseExact(valueString, "O", null, DateTimeStyles.AssumeUniversal, out dat))
                obj = dat;
            else obj = valueString;
            return obj;
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            var type = typeof(T);
            if (type != Resource.TargetType && !Resource.IsDDictionary)
            {
                var newTypeProperties = type.GetStaticProperties();
                RemoveAll(cond => newTypeProperties.All(prop => prop.Name
                                                                != cond.PropertyChain.FirstOrDefault()?.Name));
                ForEach(condition => condition.Migrate(type));
                Resource = type.GetIResource();
            }
            return entities.Where(entity => this.All(condition => condition.HoldsFor(entity)));
        }

        public WhereClause ToWhereClause()
        {
            if (!this.Any()) return new WhereClause();
            var stringPart = new List<string>();
            var valuesPart = new List<object>();
            SQL.ForEach(c =>
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