using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.NotFound;
using RESTar.WebSockets;
using static RESTar.Deflection.TermBindingRules;
using static RESTar.Operators;
using static RESTar.WebSocketStatus;

namespace RESTar.Internal
{
    internal class TerminalResource : IResource<ITerminal>, IResourceInternal, ITerminalResource
    {
        public string Name { get; }
        public Type Type { get; }
        public IReadOnlyList<Methods> AvailableMethods { get; set; }
        public string Alias { get; private set; }
        public bool IsInternal { get; }
        public bool IsGlobal { get; }
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool Equals(IEntityResource x, IEntityResource y) => x?.Name == y?.Name;
        public int GetHashCode(IEntityResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IEntityResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public TermBindingRules ConditionBindingRule { get; }
        public string Description { get; set; }
        public bool GETAvailableToAll { get; }
        public override string ToString() => Name;
        public override bool Equals(object obj) => obj is TerminalResource t && t.Name == Name;
        public override int GetHashCode() => Name.GetHashCode();
        public IReadOnlyList<IEntityResource> InnerResources { get; set; }
        public Selector<ITerminal> Select { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        private Constructor<ITerminal> Constructor { get; }
        public void SetAlias(string alias) => Alias = alias;
        public Type InterfaceType { get; }

        internal void InstantiateFor(IWebSocketInternal webSocket, IEnumerable<UriCondition> assignments = null)
        {
            var newTerminal = Constructor();
            assignments?.ForEach(assignment =>
            {
                if (assignment.Operator.OpCode != EQUALS)
                    throw new BadConditionOperator(this, assignment.Operator);
                if (!Members.TryGetValue(assignment.Key, out var property))
                {
                    if (newTerminal is IDynamicTerminal dynTerminal)
                        dynTerminal[assignment.Key] = assignment.ValueLiteral.ParseConditionValue();
                    else throw new UnknownProperty(Type, assignment.Key);
                }
                else property.SetValue(newTerminal, assignment.ValueLiteral.ParseConditionValue(property));
            });
            newTerminal.WebSocket = webSocket;
            webSocket.Terminal = newTerminal;
            webSocket.TerminalResource = this;
            switch (webSocket.Status)
            {
                case Waiting:
                    webSocket.Open();
                    break;
                case Open: break;
                case var closed:
                    throw new InvalidOperationException($"Unable to instantiate terminal '{Name}' " +
                                                        $"for a WebSocket with status '{closed}'");
            }
            newTerminal.Open();
        }

        internal static void RegisterTerminalTypes(List<Type> terminalTypes)
        {
            terminalTypes
                .OrderBy(t => t.FullName)
                .ForEach(type => RESTarConfig.AddResource(new TerminalResource(type)));
            Shell.TerminalResource = (TerminalResource) TerminalResource<Shell>.Get;
        }

        public TerminalResource(Type type)
        {
            Name = type.FullName ?? throw new Exception();
            Type = type;
            AvailableMethods = new[] {Methods.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = type.GetAttribute<RESTarAttribute>();
            InterfaceType = attribute?.Interface;
            ConditionBindingRule = type.Implements(typeof(IDynamicTerminal))
                ? DeclaredWithDynamicFallback
                : OnlyDeclared;
            Description = attribute?.Description;
            Select = null;
            Members = type.GetDeclaredProperties();
            Constructor = type.MakeStaticConstructor<ITerminal>();
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