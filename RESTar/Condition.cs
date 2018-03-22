using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error.BadRequest;
using static System.StringComparison;
using static RESTar.Operators;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// A condition encodes a predicate that is either true or false of an entity
    /// in a resource. It is used to match entities in resources while selecting 
    /// entities to include in a GET, PUT, PATCH or DELETE request.
    /// </summary>
    public class Condition<T> : ICondition where T : class
    {
        private static readonly IDictionary<UriCondition, Condition<T>> ConditionCache;
        static Condition() => ConditionCache = new ConcurrentDictionary<UriCondition, Condition<T>>(UriCondition.EqualityComparer);

        /// <inheritdoc />
        public string Key => Term.Key;

        private Operators _operator;

        /// <summary>
        /// The operator of the condition, specifies the operation of the truth
        /// evaluation. Should the condition check for equality, for example?
        /// </summary>
        public Operators Operator
        {
            get => _operator;
            set
            {
                if (value == _operator) return;
                switch (value)
                {
                    case All:
                    case None: throw new ArgumentException($"Invalid condition operator '{value}'");
                }
                _operator = value;
                if (!ScQueryable) return;
                HasChanged = true;
            }
        }

        internal Operator InternalOperator => Operator;

        private object _value;

        /// <summary>
        /// The second operand for the operation defined by the operator. Defines
        /// the object for comparison.
        /// </summary>
        public dynamic Value
        {
            get => _value;
            set
            {
                if (Do.Try(() => value == _value, false))
                    return;
                var oldValue = _value;
                _value = value;
                if (!ScQueryable) return;
                if (value == null || oldValue == null)
                    HasChanged = true;
                else ValueChanged = true;
            }
        }

        /// <inheritdoc />
        public Term Term { get; }

        /// <inheritdoc />
        public override int GetHashCode() => typeof(T).GetHashCode() + Key.GetHashCode() + Operator.GetHashCode();

        private bool _skip;

        /// <summary>
        /// Should this condition be skipped during evaluation?
        /// </summary>
        public bool Skip
        {
            get => _skip;
            set
            {
                if (ScQueryable)
                {
                    if (value == _skip) return;
                    _skip = value;
                    HasChanged = true;
                }
                else _skip = value;
            }
        }

        /// <summary>
        /// If true, this condition needs to be written to new SQL
        /// </summary>
        internal bool HasChanged { get; set; }

        /// <summary>
        /// If true, a new value must be obtained from this condition before SQL
        /// </summary>
        internal bool ValueChanged { get; set; }

        /// <summary>
        /// Is this condition queryable using Starcounter SQL?
        /// </summary>
        internal bool ScQueryable => Term.ScQueryable;

        internal Type Type => Term.IsStatic ? Term.LastAs<DeclaredProperty>()?.Type : null;
        internal bool IsOfType<T1>() => Type == typeof(T1);

        /// <inheritdoc />
        [Pure]
        public Condition<T1> Redirect<T1>(string newKey = null) where T1 : class
        {
            return new Condition<T1>
            (
                term: EntityResource<T1>.SafeGet?.MakeConditionTerm(newKey ?? Key)
                      ?? typeof(T1).MakeOrGetCachedTerm(newKey ?? Key, TermBindingRules.DeclaredWithDynamicFallback),
                op: Operator,
                value: Value
            );
        }

        /// <summary>
        /// Creates a new condition for the resource type T using a key, operator and value
        /// </summary>
        /// <param name="key">The key of the property of T to target, e.g. "Name", "Name.Length"</param>
        /// <param name="op">The operator denoting the operation to evaluate for the property</param>
        /// <param name="value">The value to compare the property referenced by the key with</param>
        public Condition(string key, Operators op, object value) : this(
            term: EntityResource<T>.SafeGet?.MakeConditionTerm(key)
                  ?? typeof(T).MakeOrGetCachedTerm(key, TermBindingRules.DeclaredWithDynamicFallback),
            op: op,
            value: value
        ) { }

        internal Condition(Term term, Operators op, object value)
        {
            Term = term;
            _operator = op;
            _value = value;
            _skip = term.ConditionSkip;
            if (!ScQueryable) return;
            HasChanged = true;
        }

        /// <summary>
        /// Returns true if and only if the condition holds for the given subject
        /// </summary>
        public bool HoldsFor(T subject)
        {
            if (Skip) return true;
            var subjectValue = Term.Evaluate(subject);

            switch (Operator)
            {
                case EQUALS: return Do.Try<bool>(() => subjectValue == Value, false);
                case NOT_EQUALS: return Do.Try<bool>(() => subjectValue != Value, true);
                case LESS_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, Ordinal) < 0;
                        return subjectValue < Value;
                    }, false);
                case GREATER_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, Ordinal) > 0;
                        return subjectValue > Value;
                    }, false);
                case LESS_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, Ordinal) <= 0;
                        return subjectValue <= Value;
                    }, false);
                case GREATER_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, Ordinal) >= 0;
                        return subjectValue >= Value;
                    }, false);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal static List<Condition<T>> Parse(string conditionsString, ITarget<T> target) => Parse(UriCondition.ParseMany(conditionsString), target);

        /// <summary>
        /// Parses and checks the semantics of Conditions object from a conditions of a REST request URI
        /// </summary>
        public static List<Condition<T>> Parse(ICollection<UriCondition> uriConditions, ITarget<T> target)
        {
            var list = new List<Condition<T>>(uriConditions.Count);
            list.AddRange(uriConditions.Select(c =>
            {
                if (ConditionCache.TryGetValue(c, out var cond))
                    return cond;
                var term = target.MakeConditionTerm(c.Key);
                var last = term.Last;
                if (!last.AllowedConditionOperators.HasFlag(c.Operator.OpCode))
                    throw new BadConditionOperator(c.Key, target, c.Operator, term, last.AllowedConditionOperators.ToOperators());
                return ConditionCache[c] = new Condition<T>
                (
                    term: term,
                    op: c.Operator.OpCode,
                    value: c.ValueLiteral.ParseConditionValue(last as DeclaredProperty)
                );
            }));
            return list;
        }
    }
}