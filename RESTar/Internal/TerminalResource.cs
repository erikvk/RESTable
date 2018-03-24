﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Deflection.Dynamic;
using RESTar.Operations;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.NotFound;
using static RESTar.Deflection.TermBindingRules;
using static RESTar.Operators;
using static RESTar.WebSocketStatus;

namespace RESTar.Internal
{
    internal class TerminalResource<T> : IResource<T>, IResourceInternal, ITerminalResource<T>, ITerminalResourceInternal<T> where T : class, ITerminal
    {
        public string Name { get; }
        public Type Type { get; }
        public IReadOnlyList<Method> AvailableMethods { get; set; }
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
        public override bool Equals(object obj) => obj is TerminalResource<T> t && t.Name == Name;
        public override int GetHashCode() => Name.GetHashCode();
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public Selector<T> Select { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        private Constructor<ITerminal> Constructor { get; }
        public void SetAlias(string alias) => Alias = alias;
        public Type InterfaceType { get; }

        public void InstantiateFor(WebSocket webSocket) => InstantiateFor(webSocket, null);

        public void InstantiateFor(WebSocket webSocket, IRequest<T> upgradeRequest)
        {
            var newTerminal = Constructor();
            upgradeRequest?.Conditions?.ForEach(assignment =>
            {
                if (assignment.Operator != EQUALS)
                    throw new BadConditionOperator(this, assignment.Operator);
                if (!Members.TryGetValue(assignment.Key, out var property))
                {
                    if (newTerminal is IDynamicTerminal dynTerminal)
                        dynTerminal[assignment.Key] = assignment.Value;
                    else throw new UnknownProperty(Type, assignment.Key);
                }
                else property.SetValue(newTerminal, assignment.Value);
            });
            
            webSocket.ReleaseTerminal();
            webSocket.TerminalConnection = new WebSocketConnection(webSocket, this, newTerminal);
            switch (webSocket.Status)
            {
                case Waiting:
                    webSocket.Open(upgradeRequest);
                    break;
                case Open: break;
                case var other:
                    throw new InvalidOperationException($"Unable to instantiate terminal '{Name}' " +
                                                        $"for a WebSocket with status '{other}'");
            }
            newTerminal.Open();
        }

        internal TerminalResource()
        {
            Name = typeof(T).FullName ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            IsInternal = false;
            IsGlobal = true;
            var attribute = typeof(T).GetAttribute<RESTarAttribute>();
            InterfaceType = attribute?.Interface;
            ConditionBindingRule = typeof(T).Implements(typeof(IDynamicTerminal))
                ? DeclaredWithDynamicFallback
                : OnlyDeclared;
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