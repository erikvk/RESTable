using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Internal;

namespace RESTar.Deflection
{
    public class PropertyChain : List<Property>
    {
        public string Key => string.Join(".", this.Select(p => p.Name));
        public string DbKey => string.Join(".", this.Select(p => p.DatabaseQueryName));
        public bool ScQueryable => this.All(p => p.ScQueryable);
        private static readonly NoCaseComparer Comparer = new NoCaseComparer();
        public bool IsStatic => this.All(p => p is StaticProperty);
        public bool IsDynamic => !IsStatic;

        internal static PropertyChain Parse(string keyString, IResource resource, List<string> dynamicDomain = null)
        {
            var chain = new PropertyChain();
            Property propertyMaker(string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException(ErrorCodes.InvalidConditionSyntaxError, $"Invalid condition '{str}'");
                if (dynamicDomain?.Contains(str, Comparer) == true)
                    return DynamicProperty.Parse(str);
                var previous = chain.LastOrDefault();
                if (previous == null)
                    return Property.Parse(str, resource.TargetType, resource.IsDDictionary);
                if (previous is StaticProperty _static)
                {
                    if (_static.Type.IsSubclassOf(typeof(DDictionary)))
                        return DynamicProperty.Parse(str);
                    return StaticProperty.Parse(str, _static.Type);
                }
                return DynamicProperty.Parse(str);
            }
            keyString.Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            return chain;
        }

        internal static PropertyChain MakeFromPrototype(PropertyChain chain, Type type)
        {
            var newChain = new PropertyChain();
            chain.ForEach(item =>
            {
                var newProp = type.MatchProperty(item.Name, false);
                newChain.Add(newProp);
                type = newProp.Type;
            });
            return newChain;
        }

        internal void MakeDynamic()
        {
            var newProperties = this.Select(prop =>
                {
                    if (prop is StaticProperty stat && !(stat is SpecialProperty))
                        return new DynamicProperty(prop.Name);
                    return prop;
                })
                .ToList();
            Clear();
            AddRange(newProperties);
        }

        internal dynamic Get(object obj)
        {
            if (obj is IDictionary<string, dynamic>)
                MakeDynamic();
            foreach (var prop in this)
            {
                if (obj == null) return null;
                obj = prop.Get(obj);
            }
            return obj;
        }
    }
}