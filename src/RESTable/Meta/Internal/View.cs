using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal;

/// <inheritdoc cref="IView" />
/// <summary>
///     Represents a RESTable resource view
/// </summary>
internal class View<TResource> : IView, ITarget<TResource> where TResource : class
{
    internal View(Type viewType, TypeCache typeCache)
    {
        EntityResource = null!; // Set later

        var viewAttribute = viewType.GetCustomAttribute<RESTableViewAttribute>();
        Type = viewType;
        Name = viewAttribute!.CustomName ?? viewType.Name;
        ViewSelector = DelegateMaker.GetDelegate<ViewSelector<TResource>>(viewType)!;
        AsyncViewSelector = DelegateMaker.GetDelegate<AsyncViewSelector<TResource>>(viewType)!;
        Members = typeCache.GetDeclaredProperties(viewType);
        Description = viewAttribute.Description;
        ConditionBindingRule = viewAttribute.AllowDynamicConditions
            ? TermBindingRule.DeclaredWithDynamicFallback
            : TermBindingRule.OnlyDeclared;
    }

    private AsyncViewSelector<TResource> AsyncViewSelector { get; }
    private ViewSelector<TResource> ViewSelector { get; }

    public IAsyncEnumerable<TResource> SelectAsync(IRequest<TResource> request, CancellationToken cancellationToken)
    {
        return AsyncViewSelector(request, cancellationToken);
    }

    /// <inheritdoc />
    /// <summary>
    ///     The binding rule to use when binding conditions to this view
    /// </summary>
    public TermBindingRule ConditionBindingRule { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public Type Type { get; }

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public IEntityResource EntityResource { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, DeclaredProperty> Members { get; }

    public IEnumerable<TResource> Select(IRequest<TResource> request)
    {
        return ViewSelector(request);
    }

    public void SetEntityResource(IEntityResource resource)
    {
        EntityResource = resource;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{EntityResource.Name}-{Name}";
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is View<TResource> view && view.Name == Name;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
