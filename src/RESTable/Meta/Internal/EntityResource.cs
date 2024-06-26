﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal;

internal class EntityResource<T> : IEntityResource<T>, IResourceInternal where T : class
{
    /// <summary>
    ///     All resources are constructed here
    /// </summary>
    internal EntityResource
    (
        string fullName,
        RESTableAttribute attribute,
        DelegateSet<T> delegates,
        IEntityResourceProviderInternal provider,
        View<T>[] views,
        TypeCache typeCache,
        ResourceCollection resourceCollection
    )
    {
        var typeName = typeof(T).FullName;
        if (typeName?.Contains('+') == true)
        {
            IsInnerResource = true;
            var location = typeName.LastIndexOf('+');
            ParentResourceName = typeName[..location].Replace('+', '.');
            Name = typeName.Replace('+', '.');
        }
        else
        {
            Name = fullName;
            ParentResourceName = null;
        }
        InnerResources = new List<IResource>();
        provider.ModifyResourceAttribute(typeof(T), attribute);
        IsDeclared = attribute.IsDeclared;
        Description = attribute.Description;
        AvailableMethods = attribute.AvailableMethods;
        IsInternal = attribute is RESTableInternalAttribute;
        InterfaceType = typeof(T).GetRESTableInterfaceType();
        (DynamicConditionsAllowed, ConditionBindingRule) = typeof(T).GetDynamicConditionHandling(attribute);
        DeclaredPropertiesFlagged = typeof(T).IsDictionary(out _, out _) || typeof(IDynamicMemberValueProvider).IsAssignableFrom(typeof(T));
        GETAvailableToAll = attribute.GETAvailableToAll;
        ResourceKind = ResourceKind.EntityResource;
        OutputBindingRule = typeof(T).GetOutputBindingRule();
        RequiresValidation = typeof(IValidator<>).IsAssignableFrom(typeof(T));
        IsDDictionary = false;
        IsDynamic = IsDDictionary || typeof(IDictionary).IsAssignableFrom(typeof(T));
        Provider = provider.Id;
        Members = typeCache.GetDeclaredProperties(typeof(T));
        Delegates = delegates;
        ViewDictionaryInternal = new Dictionary<string, ITarget<T>>(StringComparer.OrdinalIgnoreCase);
        foreach (var view in views)
        {
            if (ViewDictionaryInternal.ContainsKey(view.Name))
                throw new InvalidResourceViewDeclarationException(view.Type, $"Found multiple views with name '{view.Name}'.");
            ViewDictionaryInternal[view.Name] = view;
            view.SetEntityResource(this);
        }
        CheckOperationsSupport();
        resourceCollection.AddResource(this);
    }

    private Dictionary<string, ITarget<T>> ViewDictionaryInternal { get; }

    private List<IResource> InnerResources { get; }

    private DelegateSet<T> Delegates { get; }
    public string Name { get; }
    public IReadOnlyCollection<Method> AvailableMethods { get; private set; }
    public string? Description { get; private set; }
    public Type Type => typeof(T);
    public bool IsDDictionary { get; }
    public bool IsDynamic { get; }
    public bool IsInternal { get; }
    public bool IsGlobal => !IsInternal;
    public bool IsInnerResource { get; }
    public string? ParentResourceName { get; }
    public bool DynamicConditionsAllowed { get; }
    public IReadOnlyDictionary<string, ITarget<T>> ViewDictionary => ViewDictionaryInternal;
    public IEnumerable<ITarget> Views => ViewDictionaryInternal.Values;
    public TermBindingRule ConditionBindingRule { get; }
    public TermBindingRule OutputBindingRule { get; }
    public bool GETAvailableToAll { get; }
    public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }
    public Type? InterfaceType { get; }
    public bool DeclaredPropertiesFlagged { get; }
    public string Provider { get; }

    public bool ClaimedBy<T1>() where T1 : IEntityResourceProvider
    {
        return Provider == typeof(T1).GetEntityResourceProviderId();
    }

    public ResourceKind ResourceKind { get; }
    public bool IsDeclared { get; }
    public bool RequiresValidation { get; }

    public bool RequiresAuthentication => Delegates.RequiresAuthentication;
    public bool CanSelect => Delegates.CanSelect;
    public bool CanInsert => Delegates.CanInsert;
    public bool CanUpdate => Delegates.CanUpdate;
    public bool CanDelete => Delegates.CanDelete;
    public bool CanCount => Delegates.CanCount;

    public IAsyncEnumerable<T> SelectAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Delegates.SelectAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<T> InsertAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Delegates.InsertAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<T> UpdateAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Delegates.UpdateAsync(request, cancellationToken);
    }

    public ValueTask<long> DeleteAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Delegates.DeleteAsync(request, cancellationToken);
    }

    public ValueTask<AuthResults> AuthenticateAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Delegates.AuthenticateAsync(request, cancellationToken);
    }

    public ValueTask<long> CountAsync(IRequest<T> request, CancellationToken cancellationToken)
    {
        return Delegates.CountAsync(request, cancellationToken);
    }

    public IAsyncEnumerable<T> Validate(IAsyncEnumerable<T> entities, RESTableContext context, CancellationToken cancellationToken)
    {
        return Delegates.Validate(entities, context, cancellationToken);
    }

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

    public void AddInnerResource(IResource resource)
    {
        InnerResources.Add(resource);
    }

    public IEnumerable<IResource> GetInnerResources()
    {
        return InnerResources.AsReadOnly();
    }

    string? IResourceInternal.Description
    {
        set => Description = value;
    }

    IReadOnlyCollection<Method> IResourceInternal.AvailableMethods
    {
        set => AvailableMethods = value;
    }

    public override string ToString()
    {
        return Name;
    }

    private static IReadOnlyList<Method> GetAvailableMethods(Type? resource)
    {
        if (resource is null)
            return Array.Empty<Method>();
        return resource.GetCustomAttribute<RESTableAttribute>()?.AvailableMethods ?? Array.Empty<Method>();
    }

    private static RESTableOperations[] NecessaryOpDefs(IEnumerable<Method> restMethods)
    {
        var methodDefinitions = new HashSet<RESTableOperations>();
        foreach (var method in restMethods)
            switch (method)
            {
                case Method.HEAD:
                case Method.REPORT:
                case Method.GET:
                    methodDefinitions.Add(RESTableOperations.Select);
                    break;
                case Method.POST:
                    methodDefinitions.Add(RESTableOperations.Insert);
                    break;
                case Method.PUT:
                    methodDefinitions.Add(RESTableOperations.Select);
                    methodDefinitions.Add(RESTableOperations.Insert);
                    methodDefinitions.Add(RESTableOperations.Update);
                    break;
                case Method.PATCH:
                    methodDefinitions.Add(RESTableOperations.Select);
                    methodDefinitions.Add(RESTableOperations.Update);
                    break;
                case Method.DELETE:
                    methodDefinitions.Add(RESTableOperations.Select);
                    methodDefinitions.Add(RESTableOperations.Delete);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        return methodDefinitions.ToArray();
    }

    private bool HasDelegateForOperation(RESTableOperations op)
    {
        return op switch
        {
            RESTableOperations.Select => CanSelect,
            RESTableOperations.Insert => CanInsert,
            RESTableOperations.Update => CanUpdate,
            RESTableOperations.Delete => CanDelete,
            _ => throw new ArgumentOutOfRangeException(nameof(op))
        };
    }

    private void CheckOperationsSupport()
    {
        foreach (var op in NecessaryOpDefs(AvailableMethods))
            if (!HasDelegateForOperation(op))
            {
                var @interface = DelegateMaker.GetMatchingInterface(op);
                throw new InvalidResourceDeclarationException(
                    $"The '{op}' operation is needed to support method(s) {AvailableMethods.ToMethodsString()} for resource '{Name}', but " +
                    "RESTable found no implementation of the operation interface in the type declaration. Add an implementation of the " +
                    $"'{@interface.ToString().Replace("`1[T]", $"<{Name}>")}' interface to the resource's type declaration");
            }
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityResource<T> resource && resource.Name == Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
