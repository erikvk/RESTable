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
    public class PropertyChain
    {
        private readonly List<Property> Store;

        /// <summary>
        /// The path to the property, using dot notation
        /// </summary>
        public string Key => string.Join(".", Store.Select(p => p.Name));

        /// <summary>
        /// The property path for use in SQL queries
        /// </summary>
        public string DbKey => string.Join(".", Store.Select(p => p.DatabaseQueryName));

        /// <summary>
        /// Can this property chain be used to reference a property in an SQL statement?
        /// </summary>
        public bool ScQueryable { get; private set; }

        /// <summary>
        /// Is this property chain static? (Are all its properties static members?)
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Is this property chain dynamic? (Are not all its properties static members?)
        /// </summary>
        public bool IsDynamic => !IsStatic;

        /// <summary>
        /// Gets the first property in the chain, and safe casts it to T
        /// </summary>
        public T FirstAs<T>() where T : Property => Store.FirstOrDefault() as T;

        /// <summary>
        /// Gets the first property in the chain, or null of the chain is empty
        /// </summary>
        public Property First => Store.FirstOrDefault();

        /// <summary>
        /// Gets the last property in the chain, and safe casts it to T
        /// </summary>
        public T LastAs<T>() where T : Property => Store.LastOrDefault() as T;

        /// <summary>
        /// Gets the last property in the chain, or null of the chain is empty
        /// </summary>
        public Property Last => Store.LastOrDefault();

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();
        private PropertyChain() => Store = new List<Property>();

        internal static PropertyChain GetOrMake(IResource Resource, string key, bool dynamicUnknowns)
        {
            var hash = Resource.TargetType.GetHashCode() + key.ToLower().GetHashCode() +
                       dynamicUnknowns.GetHashCode();
            if (!PropertyChains.TryGetValue(hash, out PropertyChain propChain))
                propChain = PropertyChains[hash] = ParseInternal(Resource, key, dynamicUnknowns);
            return propChain;
        }

        internal static PropertyChain GetOrMake(int hash, IResource Resource, string key, bool dynamicUnknowns)
        {
            if (!PropertyChains.TryGetValue(hash, out PropertyChain propChain))
                propChain = PropertyChains[hash] = ParseInternal(Resource, key, dynamicUnknowns);
            return propChain;
        }

        public override int GetHashCode()
        {
            return 2;
        }

        /// <summary>
        /// Parses a property chain key string and returns a property chain describing it. This method
        /// is used for output property chains, that is, property chains that select property of outbound
        /// entities. They may have dynamic entities generated during the request, hence the dynamic domain.
        /// </summary>
        internal static PropertyChain ParseInternal(IResource resource, string key, bool dynamicUnknowns,
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

                switch (chain.Store.LastOrDefault())
                {
                    case null: return make(resource.TargetType);
                    case StaticProperty stat: return make(stat.Type);
                    default: return DynamicProperty.Parse(str);
                }
            }

            key.Split('.').ForEach(s => chain.Store.Add(propertyMaker(s)));
            chain.ScQueryable = chain.Store.All(p => p.ScQueryable);
            chain.IsStatic = chain.Store.All(p => p is StaticProperty);
            return chain;
        }

        /// <summary>
        /// Creates a new property chain from a prototype
        /// </summary>
        public static PropertyChain MakeFromPrototype(PropertyChain chain, Type type)
        {
            var newChain = new PropertyChain();
            chain.Store.ForEach(item =>
            {
                var newProp = StaticProperty.Get(type, item.Name);
                newChain.Store.Add(newProp);
                type = newProp.Type;
            });
            newChain.ScQueryable = newChain.Store.All(p => p.ScQueryable);
            newChain.IsStatic = newChain.Store.All(p => p is StaticProperty);
            return newChain;
        }

        /// <summary>
        /// Converts all properties in this property chain to dynamic properties
        /// </summary>
        public void MakeDynamic()
        {
            if (IsDynamic) return;
            var newProperties = Store.Select(prop =>
                {
                    if (prop is StaticProperty stat && !(stat is SpecialProperty))
                        return new DynamicProperty(prop.Name);
                    return prop;
                })
                .ToList();
            Store.Clear();
            Store.AddRange(newProperties);
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
            foreach (var prop in Store)
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