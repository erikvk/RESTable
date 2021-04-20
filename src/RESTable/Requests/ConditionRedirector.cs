﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using RESTable.Meta;

namespace RESTable.Requests
{
    public class ConditionRedirector
    {
        private ResourceCollection ResourceCollection { get; }
        private TermFactory TermFactory { get; }
        private TypeCache TypeCache { get; }

        public ConditionRedirector(ResourceCollection resourceCollection, TermFactory termFactory, TypeCache typeCache)
        {
            ResourceCollection = resourceCollection;
            TermFactory = termFactory;
            TypeCache = typeCache;
        }

        [Pure]
        public Condition<T> Redirect<T>(ICondition condition, string newKey = null) where T : class => new
        (
            term: TermFactory.MakeConditionTerm(ResourceCollection.SafeGetResource<T>(), newKey ?? condition.Key)
                  ?? TermFactory.MakeOrGetCachedTerm(typeof(T), newKey ?? condition.Key, ".", TermBindingRule.DeclaredWithDynamicFallback),
            op: condition.Operator,
            value: condition.Value
        );

        [Pure]
        public bool TryRedirect<TNew>(ICondition condition, out Condition<TNew> newCondition, string newKey = null)
            where TNew : class
        {
            try
            {
                newCondition = Redirect<TNew>(condition, newKey);
                return true;
            }
            catch
            {
                newCondition = null;
                return false;
            }
        }

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="T">The new type to target</typeparam>
        /// <returns></returns>
        [Pure]
        public IEnumerable<Condition<T>> Redirect<T>(IEnumerable<ICondition> conditions, string direct, string to) where T : class
        {
            return Redirect<T>(conditions, (direct, to));
        }

        /// <summary>
        /// Converts the condition collection to target a new resource type
        /// </summary>
        /// <typeparam name="T">The new type to target</typeparam>
        /// <returns></returns>
        [Pure]
        public IEnumerable<Condition<T>> Redirect<T>(IEnumerable<ICondition> conditions, params (string direct, string to)[] newKeyNames)
            where T : class
        {
            var props = TypeCache.GetDeclaredProperties(typeof(T));
            var newKeyNamesDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (newKeyNames == null)
                throw new ArgumentNullException(nameof(newKeyNames));
            foreach (var (direct, to) in newKeyNames)
                newKeyNamesDict[direct ?? throw new ArgumentNullException()] = to ?? throw new ArgumentNullException();
            foreach (var condition in conditions)
            {
                if (!condition.Term.IsDynamic)
                {
                    Condition<T> redirected;
                    if (newKeyNamesDict.TryGetValue(condition.Key, out var newKey))
                    {
                        if (TryRedirect(condition, out redirected, newKey: newKey))
                            yield return redirected;
                    }
                    else if (props.ContainsKey(condition.Term.First.Name) && TryRedirect(condition, out redirected))
                        yield return redirected;
                }
            }
        }
    }
}