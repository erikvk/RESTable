using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Operations;
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
        /// <inheritdoc />
        public string Key => Term.Key;

        private Operator _operator;

        /// <summary>
        /// The operator of the condition, specifies the operation of the truth
        /// evaluation. Should the condition check for equality, for example?
        /// </summary>
        public Operator Operator
        {
            get => _operator;
            set
            {
                if (value == _operator) return;
                _operator = value;
                if (!ScQueryable) return;
                HasChanged = true;
            }
        }

        private dynamic _value;

        /// <summary>
        /// The second operand for the operation defined by the operator. Defines
        /// the object for comparison.
        /// </summary>
        public dynamic Value
        {
            get => _value;
            set
            {
                if (Do.Try<bool>(() => value == _value, false))
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

        internal Type Type => Term.IsStatic ? Term.LastAs<StaticProperty>()?.Type : null;
        internal bool IsOfType<T1>() => Type == typeof(T1);

        /// <inheritdoc />
        [Pure]
        public Condition<T1> Redirect<T1>(string newKey = null) where T1 : class
        {
            return new Condition<T1>
            (
                term: Resource<T1>.SafeGet?.MakeConditionTerm(newKey ?? Key)
                      ?? typeof(T1).MakeOrGetCachedTerm(newKey ?? Key, TermBindingRules.StaticWithDynamicFallback),
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
        public Condition(string key, Operator op, object value) : this(
            term: Resource<T>.SafeGet?.MakeConditionTerm(key)
                  ?? typeof(T).MakeOrGetCachedTerm(key, TermBindingRules.StaticWithDynamicFallback),
            op: op,
            value: value
        ) { }

        internal Condition(Term term, Operator op, object value)
        {
            Term = term;
            _operator = op;
            _value = value;
            _skip = term.ConditionSkip;
            if (!ScQueryable) return;
            HasChanged = true;
        }

        internal bool HoldsFor(T subject)
        {
            if (Skip) return true;
            var subjectValue = Term.Evaluate(subject);

            switch (Operator.OpCode)
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

        private const string OpMatchChars = "<>=!";

        /// <summary>
        /// Parses a Conditions object from a conditions section of a REST request URI
        /// </summary>
        public static Condition<T>[] Parse(string conditionString, ITarget<T> target)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            return conditionString.Split('&').Select(s =>
            {
                if (s == "")
                    throw new SyntaxException(ErrorCodes.InvalidConditionSyntax, "Invalid condition syntax");

                s = s.ReplaceFirst("%3E=", ">=", out var replaced);
                if (!replaced) s = s.ReplaceFirst("%3C=", "<=", out replaced);
                if (!replaced) s = s.ReplaceFirst("%3E", ">", out replaced);
                if (!replaced) s = s.ReplaceFirst("%3C", "<", out replaced);

                var operatorCharacters = new string(s.Where(c => OpMatchChars.Contains(c)).ToArray());
                if (!Operator.TryParse(operatorCharacters, out var op))
                    throw new OperatorException(s);
                var keyValuePair = s.Split(new[] {op.Common}, StringSplitOptions.None);
                var term = target.MakeConditionTerm(WebUtility.UrlDecode(keyValuePair[0]));
                if (term.Last is StaticProperty stat &&
                    stat.GetAttribute<AllowedConditionOperatorsAttribute>()?.Operators?.Contains(op) == false)
                {
                    throw new ForbiddenOperatorException(s, target, op, term,
                        stat.GetAttribute<AllowedConditionOperatorsAttribute>()?.Operators);
                }
                var value = WebUtility.UrlDecode(keyValuePair[1]).ParseConditionValue();
                if (term.Last is StaticProperty prop && prop.Type.IsEnum && value is string)
                {
                    try
                    {
                        value = Enum.Parse(prop.Type, value);
                    }
                    catch
                    {
                        throw new SyntaxException(ErrorCodes.InvalidConditionSyntax,
                            $"Invalid string value for condition '{term.Key}'. The property type for '{prop.Name}' " +
                            $"has a predefined set of allowed values, not containing '{value}'.");
                    }
                }
                return new Condition<T>(term, op, value);
            }).ToArray();
        }
    }
}