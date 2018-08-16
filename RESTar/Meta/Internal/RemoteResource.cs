using System;
using System.Collections.Generic;
using RESTar.Admin;
using RESTar.Resources;

namespace RESTar.Meta.Internal
{
    internal class RemoteResource : IEntityResource, IResourceInternal
    {
        public string Name { get; }

        public RemoteResource(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }

        private static string ErrorMessage(string propertyName) => $"Cannot call {propertyName} on a remote resource";

        public string Description
        {
            get => throw new InvalidOperationException(ErrorMessage(nameof(Description)));
            set => throw new InvalidOperationException(ErrorMessage(nameof(Description)));
        }

        public IReadOnlyList<IResource> InnerResources
        {
            get => throw new InvalidOperationException(ErrorMessage(nameof(InnerResources)));
            set => throw new InvalidOperationException(ErrorMessage(nameof(InnerResources)));
        }

        IReadOnlyCollection<Method> IResourceInternal.AvailableMethods
        {
            set => throw new InvalidOperationException(ErrorMessage(nameof(IResourceInternal.AvailableMethods)));
        }

        public Type Type => throw new InvalidOperationException(ErrorMessage(nameof(Type)));
        public TermBindingRule ConditionBindingRule => throw new InvalidOperationException(ErrorMessage(nameof(ConditionBindingRule)));
        public IReadOnlyDictionary<string, DeclaredProperty> Members => throw new InvalidOperationException(ErrorMessage(nameof(Members)));
        public bool Equals(IResource x, IResource y) => throw new InvalidOperationException(ErrorMessage(nameof(Equals)));
        public int GetHashCode(IResource obj) => throw new InvalidOperationException(ErrorMessage(nameof(GetHashCode)));
        public int CompareTo(IResource other) => throw new InvalidOperationException(ErrorMessage(nameof(CompareTo)));
        public string Provider => throw new InvalidOperationException(ErrorMessage(nameof(Provider)));
        public bool ClaimedBy<T>() where T : IEntityResourceProvider => false;
        public bool IsDDictionary => throw new InvalidOperationException(ErrorMessage(nameof(IsDDictionary)));
        public bool IsDynamic => throw new InvalidOperationException(ErrorMessage(nameof(IsDynamic)));
        public bool IsDeclared => throw new InvalidOperationException(ErrorMessage(nameof(IsDeclared)));
        public bool DynamicConditionsAllowed => throw new InvalidOperationException(ErrorMessage(nameof(DynamicConditionsAllowed)));
        public bool DeclaredPropertiesFlagged => throw new InvalidOperationException(ErrorMessage(nameof(DeclaredPropertiesFlagged)));
        public TermBindingRule OutputBindingRule => throw new InvalidOperationException(ErrorMessage(nameof(OutputBindingRule)));
        public bool IsSingleton => throw new InvalidOperationException(ErrorMessage(nameof(IsSingleton)));
        public bool RequiresValidation => throw new InvalidOperationException(ErrorMessage(nameof(RequiresValidation)));
        public ResourceProfile ResourceProfile => throw new InvalidOperationException(ErrorMessage(nameof(ResourceProfile)));
        public IEnumerable<ITarget> Views => throw new InvalidOperationException(ErrorMessage(nameof(Views)));
        public bool RequiresAuthentication => throw new InvalidOperationException(ErrorMessage(nameof(RequiresAuthentication)));
        public IReadOnlyCollection<Method> AvailableMethods => throw new InvalidOperationException(ErrorMessage(nameof(AvailableMethods)));
        public string Alias => throw new InvalidOperationException(ErrorMessage(nameof(Alias)));
        public bool IsInternal => throw new InvalidOperationException(ErrorMessage(nameof(IsInternal)));
        public bool IsGlobal => throw new InvalidOperationException(ErrorMessage(nameof(IsGlobal)));
        public bool IsInnerResource => throw new InvalidOperationException(ErrorMessage(nameof(IsInnerResource)));
        public string ParentResourceName => throw new InvalidOperationException(ErrorMessage(nameof(ParentResourceName)));
        public bool GETAvailableToAll => throw new InvalidOperationException(ErrorMessage(nameof(GETAvailableToAll)));
        public Type InterfaceType => throw new InvalidOperationException(ErrorMessage(nameof(InterfaceType)));
        public ResourceKind ResourceKind => throw new InvalidOperationException(ErrorMessage(nameof(ResourceKind)));
        public void SetAlias(string alias) => throw new InvalidOperationException(ErrorMessage(nameof(SetAlias)));
    }
}