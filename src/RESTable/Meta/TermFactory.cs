using System;
using System.Collections.Generic;
using System.Linq;
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
                isInput: true
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
            isInput: true
        );

        /// <summary>
        /// Output terms are terms that refer to properties in RESTable output. If they refer to
        /// a property in the dynamic domain, they are not cached. 
        /// </summary>
        public Term MakeOutputTerm(IEntityResource target, string key, ICollection<string>? dynamicDomain) =>
            dynamicDomain is null
                ? MakeOrGetCachedTerm(target.Type, key, ".", false)
                : Parse(target.Type, key, ".", false, dynamicDomain);

        /// <summary>
        /// Creates a new term for the given type, with the given key, component separator and binding rule. If a term with
        /// the given key already existed, simply returns that one.
        /// </summary>
        public Term MakeOrGetCachedTerm(Type resource, string key, string componentSeparator, bool isInput)
        {
            var tuple = (resource.GetRESTableTypeName(), key.ToLower(), isInput);
            if (!TermCache.TryGetValue(tuple, out var term))
                term = TermCache[tuple] = Parse(resource, key, componentSeparator, isInput, null);
            return term;
        }

        /// <summary>
        /// Parses a term key string and returns a term describing it. All terms are created here.
        /// The main caller is TypeCache.MakeTerm, but it's also called from places that use a 
        /// dynamic domain (processors).
        /// </summary>
        public Term Parse(Type resource, string key, string componentSeparator, bool isInput, ICollection<string>? dynDomain)
        {
            var term = new Term(componentSeparator);
            foreach (var s in key.Split(componentSeparator))
            {
                term.Store.Add(AppendLink(s, dynDomain, term, resource, isInput));
            }
            term.SetCommonProperties();
            return term;
        }

        private Property AppendLink(string key, ICollection<string>? dynDomain, Term term, Type resource, bool isInput)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new InvalidSyntax(ErrorCodes.InvalidConditionSyntax, $"Invalid condition '{key}'");
            }
            if (dynDomain?.Contains(key, StringComparer.OrdinalIgnoreCase) == true)
            {
                return DynamicProperty.Parse(key);
            }
            return term.LastOrDefault() switch
            {
                null => MakeLink(resource, resource.GetBindingRule(isInput), key),
                DeclaredProperty declared => MakeLink(declared.Type, declared.Type.GetBindingRule(isInput), key),
                _ => DynamicProperty.Parse(key)
            };
        }

        private Property MakeLink(Type type, TermBindingRule bindingRule, string key)
        {
            switch (bindingRule)
            {
                case TermBindingRule.DeclaredWithDynamicFallback:
                    try
                    {
                        return TypeCache.FindDeclaredProperty(type, key);
                    }
                    catch (UnknownProperty)
                    {
                        return DynamicProperty.Parse(key);
                    }
                case TermBindingRule.OnlyDeclared:
                    try
                    {
                        return TypeCache.FindDeclaredProperty(type, key);
                    }
                    catch (UnknownProperty)
                    {
                        if (type.GetSubtypes().Any(subClass => TypeCache.TryFindDeclaredProperty(subClass, key, out _))) return DynamicProperty.Parse(key);
                        throw;
                    }
                default:
                    throw new Exception();
            }
        }
    }
}