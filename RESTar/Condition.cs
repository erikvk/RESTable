using System;
using System.Collections.Generic;
using System.Reflection;
using RESTar.Internal;
using static RESTar.Operators;
using static RESTar.Do;

namespace RESTar
{
    public class Condition
    {
        public string Key => PropertyChain.Key;
        public Operator Operator { get; set; }
        public dynamic Value { get; set; }
        internal PropertyChain PropertyChain { get; set; }
        internal bool IsStarcounterQueryable => PropertyChain.IsStarcounterQueryable;

        public void SetStaticKey(params PropertyInfo[] propertyChain)
        {
            PropertyChain = new PropertyChain();
            propertyChain.ForEach(prop => PropertyChain.Add(new StaticProperty(prop)));
        }

        public void SetDynamicKey(string locator)
        {
            PropertyChain = new PropertyChain();
            locator.Split('.').ForEach(prop => PropertyChain.Add(new DynamicProperty(prop)));
        }

        internal Condition(PropertyChain propertyChain, Operator op, dynamic value)
        {
            PropertyChain = propertyChain;
            Operator = op;
            Value = value;
        }

        internal static string GetTypeString(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int)) return "integer";
            if (type == typeof(bool)) return "boolean";
            return null;
        }

        internal bool HoldsFor(dynamic inputRoot)
        {
            switch (Operator.OpCode)
            {
                case EQUALS: return Try<bool>(() => PropertyChain.GetValue(inputRoot) == Value, false);
                case NOT_EQUALS: return Try<bool>(() => PropertyChain.GetValue(inputRoot) != Value, true);
                case LESS_THAN: return Try<bool>(() => PropertyChain.GetValue(inputRoot) < Value, false);
                case GREATER_THAN: return Try<bool>(() => PropertyChain.GetValue(inputRoot) > Value, false);
                case LESS_THAN_OR_EQUALS: return Try<bool>(() => PropertyChain.GetValue(inputRoot) <= Value, false);
                case GREATER_THAN_OR_EQUALS: return Try<bool>(() => PropertyChain.GetValue(inputRoot) >= Value, false);
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal void Migrate(Type type)
        {
            PropertyChain.Migrate(type);
        }

        public override string ToString()
        {
            return Key + Operator + Value;
        }
    }
}