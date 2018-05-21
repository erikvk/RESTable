﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.Results;

namespace RESTar.Meta.Internal
{
    internal class TerminalResource<T> : IResource<T>, IResourceInternal, ITerminalResource<T> where T : class
    {
        public string Name { get; }
        public Type Type { get; }
        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
        public string Alias { get; private set; }
        public bool IsInternal { get; }
        public bool IsGlobal { get; }
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public TermBindingRule ConditionBindingRule { get; }
        public string Description { get; set; }
        public bool GETAvailableToAll { get; }
        public override string ToString() => Name;
        public override bool Equals(object obj) => obj is TerminalResource<T> t && t.Name == Name;
        public override int GetHashCode() => Name.GetHashCode();
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public Selector<T> Select { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        private Constructor<ITerminal> Constructor { get; }
        public void SetAlias(string alias) => Alias = alias;
        public Type InterfaceType { get; }
        public ResourceKind ResourceKind { get; }

        internal ITerminal MakeTerminal(IEnumerable<Condition<T>> assignments = null)
        {
            var newTerminal = Constructor();
            assignments?.ForEach(assignment =>
            {
                if (assignment.Operator != Operators.EQUALS)
                    throw new BadConditionOperator(this, assignment.Operator);
                if (!Members.TryGetValue(assignment.Key, out var property))
                {
                    if (newTerminal is IDynamicTerminal dynTerminal)
                        dynTerminal[assignment.Key] = assignment.Value;
                    else throw new UnknownProperty(Type, assignment.Key);
                }
                else property.SetValue(newTerminal, assignment.Value);
            });
            return newTerminal;
        }

        internal TerminalResource()
        {
            Name = typeof(T).FullName ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = typeof(T).GetCustomAttribute<RESTarAttribute>();
            InterfaceType = attribute?.Interface;
            ResourceKind = ResourceKind.TerminalResource;
            ConditionBindingRule = typeof(T).Implements(typeof(IDynamicTerminal))
                ? TermBindingRule.DeclaredWithDynamicFallback
                : TermBindingRule.OnlyDeclared;
            Description = attribute?.Description;
            Select = null;
            Members = typeof(T).GetDeclaredProperties();
            Constructor = typeof(T).MakeStaticConstructor<ITerminal>();
            GETAvailableToAll = attribute?.GETAvailableToAll == true;
            if (Name.Contains('+'))
            {
                IsInnerResource = true;
                var location = Name.LastIndexOf('+');
                ParentResourceName = Name.Substring(0, location).Replace('+', '.');
                Name = Name.Replace('+', '.');
            }
        }
    }
}