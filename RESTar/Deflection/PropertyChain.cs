using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Internal;
using static RESTar.Deflection.TypeCache;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Operations.Do;

namespace RESTar.Deflection
{
    /// <summary>
    /// A chain of properties, used in queries to refer to properties and properties of properties.
    /// </summary>
    public class PropertyChain : List<Property>
    {
        /// <summary>
        /// The path to the property, using dot notation
        /// </summary>
        public string Key => string.Join(".", this.Select(p => p.Name));

        /// <summary>
        /// The property path for use in SQL queries
        /// </summary>
        public string DbKey => string.Join(".", this.Select(p => p.DatabaseQueryName));

        /// <summary>
        /// Can this property chain be used to reference a property in an SQL statement?
        /// </summary>
        public bool ScQueryable { get; private set; }

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();

        /// <summary>
        /// Is this property chain static? (Are all its properties static members?)
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Is this property chain dynamic? (Are not all its properties static members?)
        /// </summary>
        public bool IsDynamic => !IsStatic;

        internal static PropertyChain GetOrMake(IResource Resource, string key, bool dynamicUnknowns)
        {
            var hash = Resource.TargetType.GetHashCode() + key.ToLower().GetHashCode() +
                       dynamicUnknowns.GetHashCode();
            if (!PropertyChains.TryGetValue(hash, out PropertyChain propChain))
                propChain = PropertyChains[hash] = ParseInternal(key, Resource, dynamicUnknowns);
            return propChain;
        }

        /// <summary>
        /// Parses a property chain key string and returns a property chain describing it. This method
        /// is used for output property chains, that is, property chains that select property of outbound
        /// entities. They may have dynamic entities generated during the request, hence the dynamic domain.
        /// </summary>
        internal static PropertyChain ParseInternal(string keyString, IResource resource, bool dynamicUnknowns,
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
                            () => StaticProperty.Get(type, str),
                            () => DynamicProperty.Parse(str));
                    return StaticProperty.Get(type, str);
                }

                switch (chain.LastOrDefault())
                {
                    case null: return make(resource.TargetType);
                    case StaticProperty stat: return make(stat.Type);
                    default: return DynamicProperty.Parse(str);
                }
            }

            keyString.Split('.').ForEach(s => chain.Add(propertyMaker(s)));
            chain.ScQueryable = chain.All(p => p.ScQueryable);
            chain.IsStatic = chain.All(p => p is StaticProperty);
            return chain;
        }

        /// <summary>
        /// Creates a new property chain from a prototype
        /// </summary>
        public static PropertyChain MakeFromPrototype(PropertyChain chain, Type type)
        {
            var newChain = new PropertyChain();
            chain.ForEach(item =>
            {
                var newProp = StaticProperty.Get(type, item.Name);
                newChain.Add(newProp);
                type = newProp.Type;
            });
            newChain.ScQueryable = newChain.All(p => p.ScQueryable);
            newChain.IsStatic = newChain.All(p => p is StaticProperty);
            return newChain;
        }

        /// <summary>
        /// Converts all properties in this property chain to dynamic properties
        /// </summary>
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

        /// <summary>
        /// Gets the value of this property chain from a given target object
        /// </summary>
        public dynamic Get(object target) => Get(target, out string _);

        /// <summary>
        /// Gets the value of this property chain from a given target object and
        /// returns the actual key for this property (matching is case insensitive).
        /// </summary>
        public dynamic Get(object target, out string actualKey)
        {
            if (target is JObject jobj)
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
                if (target == null)
                {
                    actualKey = Key;
                    return null;
                }
                target = prop.Get(target);
            }
            actualKey = Key;
            return target;
        }
    }
}