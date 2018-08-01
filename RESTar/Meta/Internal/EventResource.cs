using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Requests;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class EventResource<TEvent, TPayload> : IEventResource<TEvent, TPayload>, IResourceInternal
        where TEvent : Event<TPayload> where TPayload : class
    {
        public string Name { get; }
        public string Description { get; set; }
        public Type Type { get; }
        public TermBindingRule ConditionBindingRule { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
        public string Alias { get; private set; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool GETAvailableToAll { get; }
        public Type InterfaceType { get; }
        public ResourceKind ResourceKind { get; }
        public IEnumerable<TEvent> Select(IRequest<TEvent> request) => throw new NotImplementedException();
        public IReadOnlyList<IResource> InnerResources { get; set; }
        public void SetAlias(string alias) => Alias = alias;
        public Type PayloadType { get; }
        public ITarget<TPayload> PayloadTarget { get; }

        public EventResource()
        {
            Name = typeof(TEvent).RESTarTypeName() ?? throw new Exception();
            Type = typeof(TEvent);
            PayloadType = typeof(TPayload);
            AvailableMethods = new Method[0];
            InterfaceType = typeof(TEvent).GetRESTarInterfaceType();
            ResourceKind = ResourceKind.EventResource;
            var attribute = typeof(TEvent).GetCustomAttribute<RESTarAttribute>();
            var payloadAttribute = typeof(TPayload).GetCustomAttribute<RESTarAttribute>();
            (_, ConditionBindingRule) = typeof(TEvent).GetDynamicConditionHandling(payloadAttribute ?? attribute);
            Description = attribute.Description;
            IsInternal = attribute is RESTarInternalAttribute;
            Members = typeof(TPayload).GetDeclaredProperties();
            GETAvailableToAll = false;
            PayloadTarget = new _PayloadTarget<TPayload>(payloadAttribute);
            var typeName = typeof(TEvent).FullName;
            if (typeName?.Contains('+') == true)
            {
                IsInnerResource = true;
                var location = typeName.LastIndexOf('+');
                ParentResourceName = typeName.Substring(0, location).Replace('+', '.');
                Name = typeName.Replace('+', '.');
            }
        }

        public override bool Equals(object obj) => obj is EventResource<TEvent, TPayload> resource && resource.Name == Name;
        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public override int GetHashCode() => Name.GetHashCode();

        private class _PayloadTarget<T> : ITarget<T> where T : class
        {
            public string Name { get; }
            public string Description { get; }
            public Type Type { get; }
            public TermBindingRule ConditionBindingRule { get; }
            public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
            public IEnumerable<T> Select(IRequest<T> request) => throw new NotImplementedException();

            public _PayloadTarget(RESTarAttribute attribute)
            {
                Name = typeof(T).RESTarTypeName();
                Description = attribute?.Description;
                Type = typeof(T);
                (_, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
                Members = typeof(T).GetDeclaredProperties();
            }
        }
    }
}