using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Operations.Do;

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

        public static PropertyChain Parse(string keyString, IResource resource, bool dynamicUnknowns,
            List<string> dynamicDomain = null)
        {
            var chain = new PropertyChain();

            Property propertyMaker(string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new SyntaxException(InvalidConditionSyntaxError, $"Invalid condition '{str}'");
                if (dynamicDomain?.Contains(str, Comparer) == true)
                    return DynamicProperty.Parse(str);

                Property make(Type type)
                {
                    if (type.IsDDictionary())
                        return DynamicProperty.Parse(str);
                    if (dynamicUnknowns)
                        return Try<Property>(
                            () => StaticProperty.Parse(str, type),
                            () => DynamicProperty.Parse(str));
                    return StaticProperty.Parse(str, type);
                }

                switch (chain.LastOrDefault())
                {
                    case null: return make(resource.TargetType);
                    case StaticProperty stat: return make(stat.Type);
                    default: return DynamicProperty.Parse(str);
                }
            }

            keyString.Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            return chain;
        }

        public static PropertyChain MakeFromPrototype(PropertyChain chain, Type type)
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
        
        public void MakeDynamic()
        {
            if (IsDynamic) return;
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

        public dynamic Get(object obj) => Get(obj, out string s);

        public dynamic Get(object obj, out string actualKey)
        {
            if (obj is JObject jobj)
            {
                var val = jobj.SafeGetNoCase(Key, out string actual);
                if (val != null)
                {
                    actualKey = actual;
                    return val.ToObject<dynamic>();
                }
                MakeDynamic();
            }
            foreach (var prop in this)
            {
                if (obj == null)
                {
                    actualKey = Key;
                    return null;
                }
                obj = prop.Get(obj);
            }
            actualKey = Key;
            return obj;
        }
    }
}