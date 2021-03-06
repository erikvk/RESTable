﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;
using RESTable.Linq;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal
{
    internal class TerminalResource<T> : IResource<T>, IResourceInternal, ITerminalResource<T> where T : class
    {
        public string Name { get; }
        public Type Type { get; }
        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
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
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public Type InterfaceType { get; }
        public ResourceKind ResourceKind { get; }
        private bool IsDynamicTerminal { get; }

        private Constructor<Terminal> Constructor { get; }

        public IAsyncEnumerable<T> SelectAsync(IRequest<T> request) => throw new InvalidOperationException();

        internal Terminal MakeTerminal(RESTableContext context, IEnumerable<Condition<T>> assignments = null)
        {
            var newTerminal = Constructor();
            assignments?.ForEach(assignment =>
            {
                if (assignment.Operator != Operators.EQUALS)
                    throw new BadConditionOperator(this, assignment.Operator);
                if (!Members.TryGetValue(assignment.Key, out var property))
                {
                    if (newTerminal is IDictionary<string, object> dynTerminal)
                        dynTerminal[assignment.Key] = assignment.Value;
                    else throw new UnknownProperty(Type, this, assignment.Key);
                }
                else property.SetValue(newTerminal, assignment.Value);
            });
            if (newTerminal is T t && t is IValidator<T> validator)
            {
                var invalidMembers = validator.Validate(t, context).ToList();
                if (invalidMembers.Count > 0)
                {
                    var invalidEntity = new InvalidEntity(null, invalidMembers);
                    throw new FailedValidation(invalidEntity);
                }
            }
            return newTerminal;
        }

        internal TerminalResource(TypeCache typeCache)
        {
            Name = typeof(T).GetRESTableTypeName() ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = typeof(T).GetCustomAttribute<RESTableAttribute>();
            InterfaceType = typeof(T).GetRESTableInterfaceType();
            ResourceKind = ResourceKind.TerminalResource;
            ConditionBindingRule = typeof(IDictionary<string, object>).IsAssignableFrom(typeof(T))
                ? TermBindingRule.DeclaredWithDynamicFallback
                : TermBindingRule.OnlyDeclared;
            Description = attribute?.Description;
            Members = typeCache.GetDeclaredProperties(typeof(T));
            Constructor = typeof(T).MakeStaticConstructor<Terminal>();
            GETAvailableToAll = attribute?.GETAvailableToAll == true;
            IsDynamicTerminal = typeof(IDictionary<string, object>).IsAssignableFrom(typeof(T));

            var typeName = typeof(T).FullName;
            if (typeName?.Contains('+') == true)
            {
                IsInnerResource = true;
                var location = typeName.LastIndexOf('+');
                ParentResourceName = typeName.Substring(0, location).Replace('+', '.');
                Name = typeName.Replace('+', '.');
            }
        }
    }
}