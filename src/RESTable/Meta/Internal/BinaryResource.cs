using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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
        Task<(Stream stream, ContentType contentType)> SelectBinary(IRequest<T> request);
    }

    internal class BinaryResource<T> : IResource<T>, IResourceInternal, IBinaryResource<T> where T : class
    {
        public string Name { get; }
        public string Description { get; set; }
        public Type Type { get; }
        public TermBindingRule ConditionBindingRule { get; }
        public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
        public bool Equals(IResource x, IResource y) => x?.Name == y?.Name;
        public int GetHashCode(IResource obj) => obj.Name.GetHashCode();
        public int CompareTo(IResource other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

        public IReadOnlyCollection<Method> AvailableMethods { get; set; }
        public string Alias { get; private set; }
        public bool IsInternal { get; }
        public bool IsGlobal => !IsInternal;
        public bool IsInnerResource { get; }
        public string ParentResourceName { get; }
        public bool GETAvailableToAll { get; }
        public Type InterfaceType { get; }

        public IAsyncEnumerable<T> SelectAsync(IRequest<T> request) => throw new InvalidOperationException();
        public IEnumerable<T> Select(IRequest<T> request) => throw new InvalidOperationException();

        public Task<(Stream stream, ContentType contentType)> SelectBinary(IRequest<T> request)
        {
            return BinarySelector(request);
        }

        public IReadOnlyList<IResource> InnerResources { get; set; }
        public ResourceKind ResourceKind { get; }
        private BinarySelector<T> BinarySelector { get; }

        internal BinaryResource(BinarySelector<T> binarySelector)
        {
            Name = typeof(T).GetRESTableTypeName() ?? throw new Exception();
            Type = typeof(T);
            AvailableMethods = new[] {Method.GET};
            var attribute = typeof(T).GetCustomAttribute<RESTableAttribute>();
            IsInternal = attribute is RESTableInternalAttribute;
            InterfaceType = typeof(T).GetRESTableInterfaceType();
            ResourceKind = ResourceKind.BinaryResource;
            (_, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
            Description = attribute.Description;
            BinarySelector = binarySelector;
            Members = typeof(T).GetDeclaredProperties();
            GETAvailableToAll = attribute.GETAvailableToAll;
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