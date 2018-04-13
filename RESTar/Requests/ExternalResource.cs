using System;
using System.Collections.Generic;
using RESTar.Admin;
using RESTar.Reflection;
using RESTar.Reflection.Dynamic;
using RESTar.Resources;

namespace RESTar.Requests
{
    internal class ExternalResource : IEntityResource, IResourceInternal
    {
        public string Name { get; internal set; }

        public string Description
        {
            get;
            set;
        }

        public Type Type { get; }
        public TermBindingRules ConditionBindingRule { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }

        public bool Equals(IResource x, IResource y)
        {
            throw new NotImplementedException();
        }

        public int GetHashCode(IResource obj)
        {
            throw new NotImplementedException();
        }

        public int CompareTo(IResource other)
        {
            throw new NotImplementedException();
        }

        public bool Editable { get; }
        public string Provider { get; }

        public bool ClaimedBy<T>() where T : ResourceProvider
        {
            throw new NotImplementedException();
        }

        public bool IsDDictionary { get; }
        public bool IsDynamic { get; }
        public bool DynamicConditionsAllowed { get; }
        public bool DeclaredPropertiesFlagged { get; }
        public TermBindingRules OutputBindingRule { get; }
        public bool IsSingleton { get; }
        public bool RequiresValidation { get; }
        public ResourceProfile ResourceProfile { get; }
        public IEnumerable<IView> Views { get; }
        public bool RequiresAuthentication { get; }

        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
        public string Alias { get; }
        public bool IsInternal { get; }
        public bool IsGlobal { get; }
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool GETAvailableToAll { get; }
        public Type InterfaceType { get; }
        public ResourceKind ResourceKind { get; }
        public IReadOnlyList<IResource> InnerResources { get; set; }

        public void SetAlias(string alias)
        {
            throw new NotImplementedException();
        }
    }
}