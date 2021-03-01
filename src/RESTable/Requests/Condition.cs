﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Resources.Operations;
using RESTable.Results;

namespace RESTable.Requests
{
    /// <inheritdoc cref="ICondition" />
    /// <inheritdoc cref="IUriCondition" />
    /// <summary>
    /// A condition encodes a predicate that is either true or false of an entity
    /// in a resource. It is used to match entities in resources while selecting 
    /// entities to include in a GET, PUT, PATCH or DELETE request.
    /// </summary>
    public class Condition<T> : ICondition, IUriCondition where T : class
    {
        /// <inheritdoc cref="ICondition" />
        /// <inheritdoc cref="IUriCondition" />
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
                    case Operators.All:
                    case Operators.None: throw new ArgumentException($"Invalid condition operator '{value}'");
                }
                _operator = value;
            }
        }

        private object _value;

        /// <summary>
        /// The second operand for the operation defined by the operator. Defines
        /// the object for comparison.
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                switch (_value = value)
                {
                    case DateTime dt:
                        ValueLiteral = dt.ToString("O");
                        break;
                    case string str:
                        ValueLiteral = str;
                        break;
                    case var other:
                        ValueLiteral = other?.ToString();
                        break;
                }
                ValueTypeCode = Type.GetTypeCode(_value?.GetType());
            }
        }

        /// <inheritdoc />
        public Term Term { get; }

        /// <summary>
        /// Should this condition be skipped during evaluation?
        /// </summary>
        public bool Skip { get; set; }

        /// <inheritdoc />
        public string ValueLiteral { get; private set; }

        /// <inheritdoc />
        public TypeCode ValueTypeCode { get; private set; }

        public Operator InternalOperator => Operator;

        internal Type Type => Term.IsDeclared ? Term.LastAs<DeclaredProperty>()?.Type : null;
        public bool IsOfType<T1>() => Type == typeof(T1);

        //   /// <summary>
        //   /// Creates a new condition for the resource type T using a key, operator and value
        //   /// </summary>
        //   /// <param name="key">The key of the property of T to target, e.g. "Name", "Name.Length"</param>
        //   /// <param name="op">The operator denoting the operation to evaluate for the property</param>
        //   /// <param name="value">The value to compare the property referenced by the key with</param>
        //   public Condition(string key, Operators op, object value) : this
        //   (
        //       term: EntityResource<T>.SafeGet?.MakeConditionTerm(key)
        //             ?? typeof(T).MakeOrGetCachedTerm(key, ".", TermBindingRule.DeclaredWithDynamicFallback),
        //       op: op,
        //       value: value
        //   ) { }

        public Condition(Term term, Operators op, object value)
        {
            Term = term;
            _operator = op;
            Value = value;
            Skip = term.ConditionSkip;
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
                case Operators.EQUALS: return Do.Try<bool>(() => subjectValue == Value, false);
                case Operators.NOT_EQUALS: return Do.Try<bool>(() => subjectValue != Value, true);
                case Operators.LESS_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, StringComparison.Ordinal) < 0;
                        return subjectValue < Value;
                    }, false);
                case Operators.GREATER_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, StringComparison.Ordinal) > 0;
                        return subjectValue > Value;
                    }, false);
                case Operators.LESS_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, StringComparison.Ordinal) <= 0;
                        return subjectValue <= Value;
                    }, false);
                case Operators.GREATER_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string s1 && Value is string s2)
                            return string.Compare(s1, s2, StringComparison.Ordinal) >= 0;
                        return subjectValue >= Value;
                    }, false);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Tries to parse the uri conditions to a list of conditions of the given type.
        /// </summary>
        /// <param name="uriConditions">The uri conditions to parse</param>
        /// <param name="target">The target to which the conditions refer</param>
        /// <param name="conditions">The parsed conditions (if successful)</param>
        /// <param name="error">The error encountered (if unsuccessful)</param>
        /// <returns>True if and only if the uri conditions were sucessfully parsed</returns>
        public static bool TryParse
        (
            IReadOnlyCollection<IUriCondition> uriConditions,
            ITarget<T> target,
            out List<Condition<T>> conditions,
            out Error error,
            TermFactory termFactory,
            ConditionCache<T> cache
        )
        {
            try
            {
                conditions = Parse(uriConditions, target, termFactory, cache);
                error = null;
                return true;
            }
            catch (Exception e)
            {
                conditions = null;
                error = e.AsError();
                return false;
            }
        }

        /// <summary>
        /// Parses and checks the semantics of Conditions object from a conditions of a REST request URI
        /// </summary>
        public static List<Condition<T>> Parse(IReadOnlyCollection<IUriCondition> uriConditions, ITarget<T> target, TermFactory termFactory, ConditionCache<T> cache)
        {
            var list = new List<Condition<T>>(uriConditions.Count);
            list.AddRange(uriConditions.Select(c =>
            {
                if (cache.TryGetValue(c, out var cond))
                    return cond;
                var term = termFactory.MakeConditionTerm(target, c.Key);
                var last = term.Last;
                if (!last.AllowedConditionOperators.HasFlag(c.Operator))
                    throw new BadConditionOperator(c.Key, target, c.Operator, term, last.AllowedConditionOperators.ToOperators());
                return cache[c] = new Condition<T>
                (
                    term: term,
                    op: c.Operator,
                    value: c.ValueLiteral.ParseConditionValue(last as DeclaredProperty)
                );
            }));
            return list;
        }
    }
}