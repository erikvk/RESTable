using System;
using RESTar.Deflection;
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
    public class Condition<T> where T : class
    {
        /// <summary>
        /// The key of the condition, the path to a property of an entity.
        /// </summary>
        public string Key => PropertyChain.Key;

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
        /// The property chain describing the property to compare with
        /// </summary>
        internal PropertyChain PropertyChain { get; }

        /// <summary>
        /// </summary>
        public override int GetHashCode() => Key.GetHashCode() + Operator.GetHashCode();

        internal bool HasChanged { get; set; }

        internal bool ScQueryable => PropertyChain.ScQueryable;
        internal Type Type => PropertyChain.IsStatic ? PropertyChain.LastAs<StaticProperty>()?.Type : null;
        internal bool IsOfType<T1>() => Type == typeof(T1);

        /// <summary>
        /// Converts a condition to a new target type
        /// </summary>
        public Condition<TResults> For<TResults>(string newKey = null) where TResults : class
        {
            if (typeof(TResults) == typeof(T)) return this as Condition<TResults>;
            var chain = string.IsNullOrWhiteSpace(newKey)
                ? PropertyChain.MakeFromPrototype(PropertyChain, typeof(TResults))
                : typeof(TResults).MakePropertyChain(newKey, Resource<TResults>.AllowDynamicConditions);
            var newCondition = new Condition<TResults>(chain, Operator, Value);
            return newCondition;
        }

        internal Condition(PropertyChain propertyChain, Operator op, dynamic value)
        {
            PropertyChain = propertyChain;
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
            var subjectValue = PropertyChain.Evaluate(subject);
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
    }
}