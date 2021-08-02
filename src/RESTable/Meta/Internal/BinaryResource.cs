using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal
{
    internal interface IBinaryResource<T> : IBinaryResource, IResource<T> where T : class
    {
        /// <summary>
        /// Selects binary content asynchronously from a binary resource
        /// </summary>
        BinaryResult SelectBinary(IRequest<T> request);
    }

    internal class BinaryResource<T> : IResource<T>, IResourceInternal, IBinaryResource<T> where T : class
    {
        public string Name { get; }
        public string? Description { get; set; }
        public Type Type { get; }
        public TermBindingRule ConditionBindingRule { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public bool Equals(IResource? x, IResource? y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);
        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string? ParentResourceName { get; }
        public bool GETAvailableToAll { get; }
        public Type InterfaceType { get; }
        public IAsyncEnumerable<T> SelectAsync(IRequest<T> request, CancellationToken cancellationToken) => throw new InvalidOperationException();
        public BinaryResult SelectBinary(IRequest<T> request) => BinarySelector(request);
        private List<IResource> InnerResources { get; }
        public void AddInnerResource(IResource resource) => InnerResources.Add(resource);
        public IEnumerable<IResource> GetInnerResources() => InnerResources.AsReadOnly();
        public ResourceKind ResourceKind { get; }
        private BinarySelector<T> BinarySelector { get; }

        internal BinaryResource(BinarySelector<T> binarySelector, TypeCache typeCache)
        {
            Name = typeof(T).GetRESTableTypeName() ?? throw new Exception("Could not establish binary resource name");
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            var attribute = typeof(T).GetCustomAttribute<RESTableAttribute>();
            IsInternal = attribute is RESTableInternalAttribute;
            InterfaceType = typeof(T).GetRESTableInterfaceType();
            ResourceKind = ResourceKind.BinaryResource;
            InnerResources = new List<IResource>();
            Members = typeCache.GetDeclaredProperties(typeof(T));
            (_, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
            BinarySelector = binarySelector;
            Members = typeCache.GetDeclaredProperties(typeof(T));
            if (attribute is not null)
            {
                Description = attribute.Description;
                GETAvailableToAll = attribute.GETAvailableToAll;
            }
            var typeName = typeof(T).FullName;
            if (typeName?.Contains("+") == true)
            {
                IsInnerResource = true;
                var location = typeName.LastIndexOf('+');
                ParentResourceName = typeName.Substring(0, location).Replace('+', '.');
                Name = typeName.Replace('+', '.');
            }
            else ParentResourceName = null;
        }
    }
}