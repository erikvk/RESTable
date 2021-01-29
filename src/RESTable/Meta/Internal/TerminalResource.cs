using System;
using System.Collections.Generic;
using System.Reflection;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.Meta.Internal
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
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public void SetAlias(string alias) => Alias = alias;
        public Type InterfaceType { get; }
        public ResourceKind ResourceKind { get; }
        private bool IsDynamicTerminal { get; }
        
        private Constructor<ITerminal> Constructor { get; }
        
        public IEnumerable<T> Select(IRequest<T> request) => throw new InvalidOperationException();
        
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
            Name = typeof(T).GetRESTableTypeName() ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = typeof(T).GetCustomAttribute<RESTableAttribute>();
            InterfaceType = typeof(T).GetRESTableInterfaceType();
            ResourceKind = ResourceKind.TerminalResource;
            ConditionBindingRule = typeof(IDynamicTerminal).IsAssignableFrom(typeof(T))
                ? TermBindingRule.DeclaredWithDynamicFallback
                : TermBindingRule.OnlyDeclared;
            Description = attribute?.Description;
            Members = typeof(T).GetDeclaredProperties();
            Constructor = typeof(T).MakeStaticConstructor<ITerminal>();
            GETAvailableToAll = attribute?.GETAvailableToAll == true;
            IsDynamicTerminal = typeof(IDynamicTerminal).IsAssignableFrom(typeof(T));
            
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