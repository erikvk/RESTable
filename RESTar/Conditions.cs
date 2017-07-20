using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RESTar.Deflection;
using RESTar.Internal;

namespace RESTar
{
    /// <summary>
    /// A collection of conditions
    /// </summary>
    public class Conditions<T> : IEnumerable<Condition<T>> where T : class
    {
        private readonly List<Condition<T>> Store;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private const string OpMatchChars = "<>=!";
        public IEnumerator<Condition<T>> GetEnumerator() => Store.GetEnumerator();
        internal IEnumerable<Condition<T>> SQL => Store.Where(c => c.ScQueryable);
        internal Conditions<T> PostSQL => Store.Where(c => !c.ScQueryable || c.IsOfType<string>()).ToConditions();
        internal Conditions<T> Equality => Store.Where(c => c.Operator.Equality).ToConditions();
        internal Conditions<T> Compare => Store.Where(c => c.Operator.Compare).ToConditions();

        internal Conditions()
        {
            Store = new List<Condition<T>>();
            HasChanged = true;
        }

        internal bool HasPost { get; private set; }
        private bool _hasChanged;

        internal bool HasChanged
        {
            get => _hasChanged || Store.Any(c => c.HasChanged);
            set => _hasChanged = value;
        }

        internal void ResetStatus()
        {
            Store.ForEach(c => c.HasChanged = false);
            HasChanged = false;
        }

        /// <summary>
        /// Access a condition by its key (case insensitive)
        /// </summary>
        public Condition<T> this[string key] => Store.FirstOrDefault(c => c.Key.EqualsNoCase(key));

        /// <summary>
        /// Access a condition by its key (case insensitive) and operator
        /// </summary>
        public Condition<T> this[string key, Operator op] => Store
            .FirstOrDefault(c => c.Operator == op && c.Key.EqualsNoCase(key));

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="TResults">The new type to target</typeparam>
        /// <returns></returns>
        public Conditions<TResults> MakeFor<TResults>() where TResults : class
        {
            if (typeof(TResults) == typeof(T)) return this as Conditions<TResults>;
            var newConditions = new Conditions<TResults>();
            var props = typeof(TResults).GetStaticProperties().Values;
            Store.Where(cond => props.Any(prop => prop.Name == cond.Term.First?.Name))
                .Select(cond => cond.MakeFor<TResults>())
                .ForEach(newConditions.Add);
            return newConditions;
        }

        /// <summary>
        /// Removes a condition from the collection
        /// </summary>
        public void Remove(Condition<T> condition)
        {
            Store.Remove(condition);
            HasChanged = true;
        }

        /// <summary>
        /// Removes all conditions from the collection
        /// </summary>
        public void Clear() => Store.Clear();

        /// <summary>
        /// Creates and adds a new condition to the list. Only works if T is a 
        /// resource type.
        /// </summary>
        public void Add(string key, Operator op, dynamic value)
        {
            var resource = Resource<T>.Get;
            Add(new Condition<T>(resource.MakeTerm(key, resource.DynamicConditionsAllowed), op, value));
            HasChanged = true;
        }

        /// <summary>
        /// Adds a condition to the list
        /// </summary>
        /// <param name="value"></param>
        public void Add(Condition<T> value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!value.ScQueryable || value.IsOfType<string>())
                HasPost = true;
            Store.Add(value);
            HasChanged = true;
        }

        internal void AddRange(IEnumerable<Condition<T>> conditions)
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
        public static Conditions<T> Parse(string conditionString, IResource<T> resource)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            var conditions = new Conditions<T>();
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
                var term = resource.MakeTerm(keyString, resource.DynamicConditionsAllowed);
                if (term.Last is StaticProperty stat &&
                    stat.GetAttribute<AllowedConditionOperators>()?.Operators?.Contains(op) == false)
                    throw new ForbiddenOperatorException(s, resource, op, term,
                        stat.GetAttribute<AllowedConditionOperators>()?.Operators);
                var valueString = WebUtility.UrlDecode(pair[1]);
                var value = valueString.GetConditionValue();
                if (term.IsStatic && term.Last is StaticProperty prop && prop.Type.IsEnum &&
                    value is string)
                {
                    try
                    {
                        value = Enum.Parse(prop.Type, value);
                    }
                    catch
                    {
                        throw new SyntaxException(ErrorCodes.InvalidConditionSyntaxError,
                            $"Invalid string value for condition '{term.Key}'. The property type for '{prop.Name}' " +
                            $"has a predefined set of allowed values, not containing '{value}'.");
                    }
                }
                conditions.Add(new Condition<T>(term, op, value));
            });
            return conditions;
        }

        /// <summary>
        /// Applies this list of conditions to an IEnumerable of entities and returns
        /// the entities for which all the conditions hold.
        /// </summary>
        public IEnumerable<T> Apply(IEnumerable<T> entities)
        {
            return entities.Where(entity => Store.All(condition => condition.HoldsFor(entity)));
        }
    }
}