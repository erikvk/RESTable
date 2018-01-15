using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using RESTar.Operations;
using RESTar.WebSockets;

namespace RESTar.Internal
{
    internal class WebSocketSerializer : ITerminal
    {
        public IWebSocket WebSocket { get; set; }
        public void HandleTextInput(string input) { }
        public void HandleBinaryInput(byte[] input) { }
        public bool SupportsTextInput { get; } = false;
        public bool SupportsBinaryInput { get; } = false;
        public void Dispose() { }
    }

    internal class TerminalResource : IResource<ITerminal>
    {
        public string FullName { get; }
        public Type Type { get; }
        public IReadOnlyList<Methods> AvailableMethods { get; }
        public string Alias { get; set; }
        public bool IsInternal { get; }
        public bool IsGlobal { get; }
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool Equals(IEntityResource x, IEntityResource y) => x?.FullName == y?.FullName;
        public int GetHashCode(IEntityResource obj) => obj.FullName.GetHashCode();
        public int CompareTo(IEntityResource other) => string.Compare(FullName, other.FullName, StringComparison.Ordinal);
        public TermBindingRules ConditionBindingRule { get; }
        public string Description { get; }
        public override string ToString() => FullName;
        public override bool Equals(object obj) => obj is TerminalResource t && t.FullName == FullName;
        public override int GetHashCode() => FullName.GetHashCode();
        internal static TerminalResource Default { get; }
        static TerminalResource() => Default = new TerminalResource(typeof(WebSocketSerializer)) {Constructor = () => new WebSocketSerializer()};

        public Selector<ITerminal> Select { get; }
        private Constructor<ITerminal> Constructor { get; set; }

        internal void CreateFor(IWebSocketInternal webSocket)
        {
            var terminal = Constructor();
            terminal.WebSocket = webSocket;
            webSocket.Terminal = terminal;
            webSocket.TerminalResource = this;
        }

        internal static void RegisterTerminalTypes(List<Type> terminalTypes) => terminalTypes
            .OrderBy(t => t.FullName)
            .ForEach(type => RESTarConfig.AddResource(new TerminalResource(type)));

        public TerminalResource(Type type)
        {
            FullName = type.FullName ?? throw new Exception();
            Type = type;
            AvailableMethods = new[] {Methods.GET};
            IsInternal = false;
            IsGlobal = true;
            ConditionBindingRule = TermBindingRules.FreeText;
            Description = type.GetAttribute<RESTarAttribute>()?.Description;
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