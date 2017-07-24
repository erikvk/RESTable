using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Linq;
using RESTar.Deflection.Dynamic;
using RESTar.Internal;
using RESTar.Operations;
using static System.StringComparison;
using static RESTar.Operators;

namespace RESTar
{
    /// <summary>
    /// A condition encodes a predicate that is either true or false of an entity
    /// in a resource. It is used to match entities in resources while selecting 
    /// entities to include in a GET, PUT, PATCH or DELETE request.
    /// </summary>
    public class Condition<T> : ICondition where T : class
    {
        /// <summary>
        /// The key of the condition, the path to a property of an entity.
        /// </summary>
        public string Key => Term.Key;

        /// <summary>
        /// The operator of the condition, specifies the operation of the truth
        /// evaluation. Should the condition check for equality, for example?
        /// </summary>
        public Operator Operator { get; private set; }

        /// <summary>
        /// The second operand for the operation defined by the operator. Defines
        /// the object for comparison.
        /// </summary>
        public dynamic Value { get; private set; }

        /// <summary>
        /// The term describing the property to compare with
        /// </summary>
        public Term Term { get; }

        /// <summary>
        /// </summary>
        public override int GetHashCode() => typeof(T).GetHashCode() + Key.GetHashCode() + Operator.GetHashCode();

        /// <summary>
        /// Should this condition be skipped during evaluation?
        /// </summary>
        public bool Skip { get; set; }

        internal bool HasChanged { get; set; }
        internal bool ScQueryable => Term.ScQueryable;
        internal Type Type => Term.IsStatic ? Term.LastAs<StaticProperty>()?.Type : null;
        internal bool IsOfType<T1>() => Type == typeof(T1);

        /// <summary>
        /// Converts a condition to a new target type
        /// </summary>
        [Pure]
        public Condition<T1> Redirect<T1>(string newKey = null) where T1 : class => new Condition<T1>
        (
            term: typeof(T1).MakeTerm
            (
                key: newKey ?? Key,
                dynamicUnknowns: Resource<T1>.AllowDynamicConditions
            ),
            op: Operator,
            value: Value
        );

        internal Condition(Term term, Operator op, dynamic value)
        {
            Term = term;
            Operator = op;
            Value = value;
            HasChanged = true;
        }

        /// <summary>
        /// Sets the condition operator and returns the changed condition
        /// </summary>
        public Condition<T> SetOperator(Operator newOperator)
        {
            Operator = newOperator;
            HasChanged = true;
            return this;
        }

        /// <summary>
        /// Sets the condition value and returns the changed condition
        /// </summary>
        public Condition<T> SetValue(dynamic value)
        {
            Value = value;
            HasChanged = true;
            return this;
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
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, Ordinal) < 0;
                        return subjectValue < Value;
                    }, false);
                case GREATER_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, Ordinal) > 0;
                        return subjectValue > Value;
                    }, false);
                case LESS_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, Ordinal) <= 0;
                        return subjectValue <= Value;
                    }, false);
                case GREATER_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, Ordinal) >= 0;
                        return subjectValue >= Value;
                    }, false);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private const string OpMatchChars = "<>=!";

        /// <summary>
        /// Parses a Conditions object from a conditions section of a REST request URI
        /// </summary>
        public static Condition<T>[] Parse(string conditionString, IResource<T> resource)
        {
            if (string.IsNullOrEmpty(conditionString)) return null;
            return conditionString.Split('&').Select(s =>
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
                return new Condition<T>(term, op, value);
            }).ToArray();
        }
    }
}