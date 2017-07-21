using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using static RESTar.Internal.ErrorCodes;
using static RESTar.Operations.Do;

namespace RESTar.Deflection
{
    /// <summary>
    /// A term denotes a node in a static or dynamic member tree. Contains a chain of properties, 
    /// used in queries to refer to properties and properties of properties.
    /// </summary>
    public class Term
    {
        private readonly List<Property> Store;

        /// <summary>
        /// A string representation of the path to the property, using dot notation
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The property path for use in SQL queries
        /// </summary>
        public string DbKey => string.Join(".", Store.Select(p => p.DatabaseQueryName));

        /// <summary>
        /// Can this term be used to reference a property in an SQL statement?
        /// </summary>
        public bool ScQueryable { get; private set; }

        /// <summary>
        /// Is this term static? (Are all of its containing property references denoting static members?)
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Is this term dynamic? (Are not all of its containing property references denoting static members?)
        /// </summary>
        public bool IsDynamic => !IsStatic;

        /// <summary>
        /// Gets the first property reference of the term, and safe casts it to T
        /// </summary>
        public T FirstAs<T>() where T : Property => Store.FirstOrDefault() as T;

        /// <summary>
        /// Gets the first property reference of the term, or null of the term is empty
        /// </summary>
        public Property First => Store.FirstOrDefault();

        /// <summary>
        /// Gets the last property reference of the term, and safe casts it to T
        /// </summary>
        public T LastAs<T>() where T : Property => Store.LastOrDefault() as T;

        /// <summary>
        /// Gets the last property reference of the term, or null of the term is empty
        /// </summary>
        public Property Last => Store.LastOrDefault();

        private static readonly NoCaseComparer Comparer = new NoCaseComparer();
        private Term() => Store = new List<Property>();

        /// <summary>
        /// Parses a term key string and returns a term describing it.
        /// </summary>
        internal static Term ParseInternal(Type resource, string key, bool dynamicUnknowns,
            List<string> dynamicDomain = null)
        {
            var term = new Term();

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

                switch (term.Store.LastOrDefault())
                {
                    case null: return make(resource);
                    case StaticProperty stat: return make(stat.Type);
                    default: return DynamicProperty.Parse(str);
                }
            }

            key.Split('.').ForEach(s => term.Store.Add(propertyMaker(s)));
            term.ScQueryable = term.Store.All(p => p.ScQueryable);
            term.IsStatic = term.Store.All(p => p is StaticProperty);
            term.Key = string.Join(".", term.Store.Select(p => p.Name));
            return term;
        }

        /// <summary>
        /// Creates a new term from a prototype
        /// </summary>
        public static Term MakeFromPrototype(Term term, Type type)
        {
            var newTerm = new Term();
            term.Store.ForEach(item =>
            {
                var newProp = StaticProperty.Get(type, item.Name);
                newTerm.Store.Add(newProp);
                type = newProp.Type;
            });
            newTerm.ScQueryable = newTerm.Store.All(p => p.ScQueryable);
            newTerm.IsStatic = newTerm.Store.All(p => p is StaticProperty);
            newTerm.Key = string.Join(".", newTerm.Store.Select(p => p.Name));
            return newTerm;
        }

        /// <summary>
        /// Converts all properties in this term to dynamic properties
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
            Key = string.Join(".", Store.Select(p => p.Name));
        }

        /// <summary>
        /// Returns the value that this term denotes for a given target object
        /// </summary>
        public dynamic Evaluate(object target) => Evaluate(target, out string _);

        /// <summary>
        /// Returns the value that this term denotes for a given target object as well as
        /// the actual key for this property (matching is case insensitive).
        /// </summary>
        public dynamic Evaluate(object target, out string actualKey)
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