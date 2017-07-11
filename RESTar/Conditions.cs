using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar
{
    /// <summary>
    /// A collection of conditions
    /// </summary>
    public sealed class Conditions : IEnumerable<Condition>, IFilter
    {
        private readonly List<Condition> Store;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<Condition> GetEnumerator() => Store.GetEnumerator();

        internal IResource Resource;
        internal IEnumerable<Condition> SQL => Store.Where(c => c.ScQueryable);
        internal Conditions PostSQL => Store.Where(c => !c.ScQueryable || c.IsOfType<string>()).ToConditions(Resource);
        internal Conditions Equality => Store.Where(c => c.Operator.Equality).ToConditions(Resource);
        internal Conditions Compare => Store.Where(c => c.Operator.Compare).ToConditions(Resource);
        private static readonly char[] OpMatchChars = {'<', '>', '=', '!'};

        internal Conditions(IResource resource)
        {
            Resource = resource;
            Store = new List<Condition>();
            HasChanged = true;
        }

        internal bool HasPost { get; private set; }
        internal int SqlHash { get; private set; }
        internal bool HasChanged { get; private set; }

        internal int Prep()
        {
            unchecked
            {
                SqlHash = Resource.Name.GetHashCode() +
                          SQL.Select((item, index) => item.Prep() + index)
                              .Aggregate(17, (a, b) => a + b * 23);
            }
            HasChanged = false;
            return SqlHash;
        }

        /// <summary>
        /// Access a condition by its key (case insensitive)
        /// </summary>
        public Condition this[string key] => Store.FirstOrDefault(c => c.Key.EqualsNoCase(key));

        /// <summary>
        /// Access a condition by its key (case insensitive) and operator
        /// </summary>
        public Condition this[string key, Operator op] => Store
            .FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key));

        /// <summary>
        /// Adds a condition to the list
        /// </summary>
        /// <param name="value"></param>
        internal void Add(Condition value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!value.ScQueryable || value.IsOfType<string>())
                HasPost = true;
            Store.Add(value);
            HasChanged = true;
        }

        /// <summary>
        /// Removes a condition from the collection
        /// </summary>
        public void Remove(Condition condition)
        {
            Store.Remove(condition);
            HasChanged = true;
        }

        /// <summary>
        /// Creates and adds a new condition to the list
        /// </summary>
        public void Add(string key, Operator op, dynamic value)
        {
            Add(new Condition(PropertyChain.GetOrMake(Resource, key, Resource.DynamicConditionsAllowed), op, value));
            HasChanged = true;
        }

        internal void AddRange(IEnumerable<Condition> conditions)
        {
            Store.AddRange(conditions);
            HasChanged = true;
        }

        /// <summary>
        /// True if and only if the conditions collection contains one or more elements
        /// </summary>
        public bool Any => Store.Any();

        /// <summary>
        /// Parses a Conditions object from a conditions section of a REST request URI
        /// </summary>
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
                var chain = PropertyChain.GetOrMake(resource, keyString, resource.DynamicConditionsAllowed);
                if (chain.Last is StaticProperty stat &&
                    stat.GetAttribute<AllowedConditionOperators>()?.Operators?.Contains(op) == false)
                    throw new ForbiddenOperatorException(s, resource, op, chain,
                        stat.GetAttribute<AllowedConditionOperators>()?.Operators);
                var valueString = WebUtility.UrlDecode(pair[1]);
                var value = GetValue(valueString);
                if (chain.IsStatic && chain.Last is StaticProperty prop && prop.Type.IsEnum &&
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
            if (resource.TargetType == typeof(Resource))
            {
                var nameCond = conditions["name"];
                nameCond?.SetValue(((string) nameCond.Value.ToString()).FindResource().Name);
            }
            return conditions;
        }

        internal static dynamic GetValue(string valueString)
        {
            if (valueString == null) return null;
            if (valueString == "null") return null;
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

        /// <summary>
        /// Applies this list of conditions to an IEnumerable of entities and returns
        /// the entities for which all the conditions hold.
        /// </summary>
        /// <typeparam name="T">The resource type</typeparam>
        public IEnumerable<T> Apply<T>(IEnumerable<T> entities)
        {
            var type = typeof(T);
            if (type != Resource.TargetType && !Resource.IsDDictionary)
            {
                var newTypeProperties = type.GetStaticProperties().Values;
                Store.RemoveAll(cond => newTypeProperties.All(prop =>
                    prop.Name != cond.PropertyChain.First?.Name));
                Store.ForEach(condition => condition.Migrate(type));
                Resource = RESTar.Resource.Get(type);
            }
            return entities.Where(entity => Store.All(condition => condition.HoldsFor(entity)));
        }
    }
}