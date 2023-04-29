using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal;

internal interface IBinaryResource<T> : IBinaryResource, IResource<T> where T : class
{
    /// <summary>
    ///     Selects binary content asynchronously from a binary resource
    /// </summary>
    ValueTask<BinaryResult> SelectBinaryAsync(IRequest<T> request, CancellationToken cancellationToken);
}

internal class BinaryResource<T> : IResource<T>, IResourceInternal, IBinaryResource<T> where T : class
{
    internal BinaryResource(AsyncBinarySelector<T> binarySelector, TypeCache typeCache)
    {
        Name = typeof(T).GetRESTableTypeName();
        Type = typeof(T);
        var attribute = typeof(T).GetCustomAttribute<RESTableAttribute>();
        AvailableMethods = attribute?.AvailableMethods
            .Where(m => m is Method.GET or Method.HEAD)
            .ToArray() ?? new[] { Method.GET };
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
            ParentResourceName = typeName[..location].Replace('+', '.');
            Name = typeName.Replace('+', '.');
        }
        else
        {
            ParentResourceName = null;
        }
    }

    private List<IResource> InnerResources { get; }
    private AsyncBinarySelector<T> BinarySelector { get; }

    public ValueTask<BinaryResult> SelectBinaryAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return BinarySelector(request, cancellationToken);
    }

    public string Name { get; }
    public string? Description { get; set; }
    public Type Type { get; }
    public TermBindingRule ConditionBindingRule { get; }
    public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }

    public bool Equals(IResource? x, IResource? y)
    {
        return x?.Name == y?.Name;
    }

    public int GetHashCode(IResource obj)
    {
        return obj.Name.GetHashCode();
    }

    public int CompareTo(IResource? other)
    {
        return string.Compare(Name, other?.Name, StringComparison.Ordinal);
    }

    public IReadOnlyCollection<Method> AvailableMethods { get; set; }
    public bool IsInternal { get; }
    public bool IsGlobal => !IsInternal;
    public bool IsInnerResource { get; }
    public string? ParentResourceName { get; }
    public bool GETAvailableToAll { get; }
    public Type? InterfaceType { get; }

    public IAsyncEnumerable<T> SelectAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException();
    }

    public ResourceKind ResourceKind { get; }

    public void AddInnerResource(IResource resource)
    {
        InnerResources.Add(resource);
    }

    public IEnumerable<IResource> GetInnerResources()
    {
        return InnerResources.AsReadOnly();
    }
}
