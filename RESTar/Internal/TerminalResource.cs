using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Fail.BadRequest;
using RESTar.Results.Fail.NotFound;
using RESTar.WebSockets;
using static RESTar.Deflection.TermBindingRules;
using static RESTar.WebSocketStatus;

namespace RESTar.Internal
{
    internal class TerminalResource : IResource<ITerminal>, IResourceInternal
    {
        public string FullName { get; }
        public Type Type { get; }
        public IReadOnlyList<Methods> AvailableMethods { get; set; }
        public string Alias { get; set; }
        public bool IsInternal { get; }
        public bool IsGlobal { get; }
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool Equals(IEntityResource x, IEntityResource y) => x?.FullName == y?.FullName;
        public int GetHashCode(IEntityResource obj) => obj.FullName.GetHashCode();
        public int CompareTo(IEntityResource other) => string.Compare(FullName, other.FullName, StringComparison.Ordinal);
        public TermBindingRules ConditionBindingRule { get; }
        public string Description { get; set; }
        public override string ToString() => FullName;
        public override bool Equals(object obj) => obj is TerminalResource t && t.FullName == FullName;
        public override int GetHashCode() => FullName.GetHashCode();
        public IReadOnlyList<IEntityResource> InnerResources { get; set; }

        public Selector<ITerminal> Select { get; }
        private Constructor<ITerminal> Constructor { get; }

        internal Dictionary<string, object> GetTerminalState(ITerminal terminal) => Type
            .GetDeclaredProperties().Values.ToDictionary(p => p.Name, p => p.GetValue(terminal));

        internal void SetTerminalState(IDictionary<string, object> state, ITerminal terminal) => Type
            .GetDeclaredProperties().Values.ForEach(p => p.SetValue(terminal, state[p.Name]));

        internal void InstantiateFor(IWebSocketInternal webSocket, ICollection<UriCondition> assignments = null)
        {
            var newTerminal = Constructor();
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
                    throw new InvalidOperationException($"Unable to instantiate terminal '{FullName}' " +
                                                        $"for a WebSocket with status '{closed}'");
            }
            if (assignments?.Any() == true)
            {
                var properties = Type.GetDeclaredProperties();
                foreach (var assignment in assignments)
                {
                    if (assignment.Operator.OpCode != Operators.EQUALS)
                        throw new InvalidSyntax(ErrorCodes.InvalidConditionOperator,
                            $"Invalid operator '{assignment.Operator.Common}' in condition to terminal resource. " +
                            "Only \'=\' is valid in terminal conditions.");
                    if (!properties.TryGetValue(assignment.Key, out var property))
                    {
                        if (newTerminal is IDynamicTerminal dynTerminal)
                            dynTerminal[assignment.Key] = assignment.ValueLiteral;
                        else throw new UnknownProperty(Type, assignment.Key);
                    }
                    else property.SetValue(newTerminal, Convert.ChangeType(assignment.ValueLiteral, property.Type));
                }
            }

            newTerminal.Open();
        }

        internal static void RegisterTerminalTypes(List<Type> terminalTypes)
        {
            terminalTypes
                .OrderBy(t => t.FullName)
                .ForEach(type => RESTarConfig.AddResource(new TerminalResource(type)));
            Shell.TerminalResource = Resource.Get(typeof(Shell)) as TerminalResource;
        }

        public TerminalResource(Type type)
        {
            FullName = type.FullName ?? throw new Exception();
            Type = type;
            AvailableMethods = new[] {Methods.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = type.GetAttribute<RESTarAttribute>();
            ConditionBindingRule = type.Implements(typeof(IDynamicTerminal))
                ? DeclaredWithDynamicFallback
                : OnlyDeclared;
            Description = attribute?.Description;
            Select = null;
            Constructor = type.MakeStaticConstructor<ITerminal>();
            if (FullName.Contains('+'))
            {
                IsInnerResource = true;
                var location = FullName.LastIndexOf('+');
                ParentResourceName = FullName.Substring(0, location).Replace('+', '.');
                FullName = FullName.Replace('+', '.');
            }
        }
    }
}