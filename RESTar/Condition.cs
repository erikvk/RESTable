﻿using System;
using System.Linq;
using RESTar.Deflection;
using RESTar.Operations;

namespace RESTar
{
    /// <summary>
    /// A condition encodes a predicate that is either true or false of an entity
    /// in a resource. It is used to match entities in resources while selecting 
    /// entities to include in a GET, PUT, PATCH or DELETE request.
    /// </summary>
    public class Condition
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
        public PropertyChain PropertyChain { get; private set; }

        internal bool ScQueryable => PropertyChain.ScQueryable;
        internal Type Type => PropertyChain.IsStatic ? ((StaticProperty) PropertyChain.Last())?.Type : null;
        internal bool IsOfType<T>() => Type == typeof(T);

        internal void Migrate(Type newType)
        {
            PropertyChain = PropertyChain.MakeFromPrototype(PropertyChain, newType);
        }

        internal Condition(PropertyChain propertyChain, Operator op, dynamic value)
        {
            PropertyChain = propertyChain;
            Operator = op;
            Value = value;
        }

        /// <summary>
        /// Used to repoint the condition towards a different static or dynamic property
        /// of a type T.
        /// </summary>
        public Condition Repoint<T>(string key)
        {
            if (!RESTarConfig.ResourceByType.TryGetValue(typeof(T), out var resource))
                throw new UnknownResourceException(typeof(T).FullName);
            PropertyChain = PropertyChain.GetOrMake(resource, key, resource.DynamicConditionsAllowed);
            return this;
        }

        /// <summary>
        /// Sets the condition operator and returns the changed condition
        /// </summary>
        public Condition SetOperator(Operator newOperator)
        {
            Operator = newOperator;
            return this;
        }

        /// <summary>
        /// Sets the condition value and returns the changed condition
        /// </summary>
        public Condition SetValue(dynamic value)
        {
            Value = value;
            return this;
        }

        internal bool HoldsFor(dynamic subject)
        {
            var subjectValue = PropertyChain.Get(subject);
            switch (Operator.OpCode)
            {
                case Operators.EQUALS: return Do.Try<bool>(() => subjectValue == Value, false);
                case Operators.NOT_EQUALS: return Do.Try<bool>(() => subjectValue != Value, true);
                case Operators.LESS_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) < 0;
                        return subjectValue < Value;
                    }, false);
                case Operators.GREATER_THAN:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) > 0;
                        return subjectValue > Value;
                    }, false);
                case Operators.LESS_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) <= 0;
                        return subjectValue <= Value;
                    }, false);
                case Operators.GREATER_THAN_OR_EQUALS:
                    return Do.Try<bool>(() =>
                    {
                        if (subjectValue is string && Value is string)
                            return string.Compare((string) subjectValue, (string) Value, StringComparison.Ordinal) >= 0;
                        return subjectValue >= Value;
                    }, false);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}