using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Meta
{
    public class TermFactory
    {
        private TypeCache TypeCache { get; }
        private TermCache TermCache { get; }
        private ResourceCollection ResourceCollection { get; }

        public TermFactory(TypeCache typeCache, TermCache termCache, ResourceCollection resourceCollection)
        {
            TypeCache = typeCache;
            TermCache = termCache;
            ResourceCollection = resourceCollection;
        }

        /// <summary>
        /// Create a new term for a given type, with a key describing the target property
        /// </summary>
        public Term Create<T>(string key) where T : class => Create(typeof(T), key);

        /// <summary>
        /// Create a new term for a given type, with a key describing the target property
        /// </summary>
        public Term Create(Type type, string key, string componentSeparator = ".") => MakeOrGetCachedTerm
        (
            resource: type,
            key: key,
            componentSeparator: componentSeparator,
            bindingRule: TermBindingRule.DeclaredWithDynamicFallback
        );

        /// <summary>
        /// Create a new term from a given PropertyInfo
        /// </summary>
        public Term Create(PropertyInfo propertyInfo) => MakeOrGetCachedTerm
        (
            resource: propertyInfo.DeclaringType,
            key: propertyInfo.Name,
            componentSeparator: ".",
            bindingRule: TermBindingRule.DeclaredWithDynamicFallback
        );

        /// <summary>
        /// Condition terms are terms that refer to properties in resources, or for
        /// use in conditions.
        /// </summary>
        public Term MakeConditionTerm<T>(string key) where T : class
        {
            var target = ResourceCollection.GetResource<T>();
            return MakeOrGetCachedTerm
            (
                resource: target.Type,
                key: key,
                componentSeparator: ".",
                bindingRule: target.ConditionBindingRule
            );
        }

        /// <summary>
        /// Condition terms are terms that refer to properties in resources, or for
        /// use in conditions.
        /// </summary>
        public Term MakeConditionTerm(ITarget target, string key) => MakeOrGetCachedTerm
        (
            resource: target.Type,
            key: key,
            componentSeparator: ".",
            bindingRule: target.ConditionBindingRule
        );

        /// <summary>
        /// Output terms are terms that refer to properties in RESTable output. If they refer to
        /// a property in the dynamic domain, they are not cached. 
        /// </summary>
        public Term MakeOutputTerm(IEntityResource target, string key, ICollection<string> dynamicDomain) =>
            dynamicDomain is null
                ? MakeOrGetCachedTerm(target.Type, key, ".", target.OutputBindingRule)
                : Parse(target.Type, key, ".", target.OutputBindingRule, dynamicDomain);

        /// <summary>
        /// Creates a new term for the given type, with the given key, component separator and binding rule. If a term with
        /// the given key already existed, simply returns that one.
        /// </summary>
        public Term MakeOrGetCachedTerm(Type resource, string key, string componentSeparator, TermBindingRule bindingRule)
        {
            var tuple = (resource.GetRESTableTypeName(), key.ToLower(), bindingRule);
            if (!TermCache.TryGetValue(tuple, out var term))
                term = TermCache[tuple] = Parse(resource, key, componentSeparator, bindingRule, null);
            return term;
        }

        /// <summary>
        /// Parses a term key string and returns a term describing it. All terms are created here.
        /// The main caller is TypeCache.MakeTerm, but it's also called from places that use a 
        /// dynamic domain (processors).
        /// </summary>
        public Term Parse(Type resource, string key, string componentSeparator, TermBindingRule bindingRule, ICollection<string> dynDomain)
        {
            var term = new Term(componentSeparator);

            Property propertyMaker(string str)
            {
                if (string.IsNullOrWhiteSpace(str))
                    throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition '{str}'");
                if (dynDomain?.Contains(str, StringComparer.OrdinalIgnoreCase) == true)
                    return DynamicProperty.Parse(str);

                Property make(Type type)
                {
                    switch (bindingRule)
                    {
                        case TermBindingRule.DeclaredWithDynamicFallback:
                            try
                            {
                                return TypeCache.FindDeclaredProperty(type, str);
                            }
                            catch (UnknownProperty)
                            {
                                return DynamicProperty.Parse(str);
                            }
                        case TermBindingRule.DynamicWithDeclaredFallback: return DynamicProperty.Parse(str, true);
                        case TermBindingRule.OnlyDeclared:
                            try
                            {
                                return TypeCache.FindDeclaredProperty(type, str);
                            }
                            catch (UnknownProperty)
                            {
                                if (type.GetSubclasses().Any(subClass => TypeCache.TryFindDeclaredProperty(subClass, str, out _)))
                                    return DynamicProperty.Parse(str);
                                throw;
                            }
                        default: throw new Exception();
                    }
                }

                return term.LastOrDefault() switch
                {
                    null => make(resource),
                    DeclaredProperty declared => make(declared.Type),
                    _ => DynamicProperty.Parse(str)
                };
            }

            foreach (var s in key.Split(componentSeparator)) 
                term.Store.Add(propertyMaker(s));
            term.SetCommonProperties();
            return term;
        }
    }
}