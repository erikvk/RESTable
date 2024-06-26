using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable;

/// <inheritdoc />
/// <summary>
///     A resource that generates a list of the available resources for the current user
/// </summary>
[RESTable(GET, Description = description, GETAvailableToAll = true)]
public sealed class AvailableResource : ISelector<AvailableResource>
{
    private const string description = "The AvailableResource resource contains all resources " +
                                       "available for the current user, as defined by the access " +
                                       "rights assigned to its API key. It is the default resource " +
                                       "used when no resource is specified in the request URI.";

    public AvailableResource(string name, string description, Method[] methods, ResourceKind kind, ViewInfo[] views, AvailableResource[] innerResources)
    {
        Name = name;
        Description = description;
        Methods = methods;
        Kind = kind;
        Views = views;
        InnerResources = innerResources;
    }

    /// <summary>
    ///     The name of the resource
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Resource descriptions are visible in the AvailableMethods resource
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     The methods that have been enabled for this resource
    /// </summary>
    public Method[] Methods { get; }

    /// <summary>
    ///     The resource type, entity resource or terminal resource
    /// </summary>
    public ResourceKind Kind { get; }

    /// <summary>
    ///     The views for this resource
    /// </summary>
    [RESTableMember(hideIfNull: true)]
    public ViewInfo[] Views { get; }

    /// <summary>
    ///     Inner resources for this resource
    /// </summary>
    [RESTableMember(hideIfNull: true)]
    public AvailableResource[] InnerResources { get; }

    /// <inheritdoc />
    public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        return request.Context.Client.AccessRights.Keys
            .Where(r => r is { IsGlobal: true, IsInnerResource: false })
            .OrderBy(r => r.Name)
            .Select(r => Make(r, request));
    }

    internal static AvailableResource Make(IResource iresource, ITraceable trace)
    {
        return new(
            iresource.Name,
            iresource.Description ?? "No description",
            trace.Context.Client.AccessRights
                .SafeGet(iresource)?
                .Intersect(iresource.AvailableMethods)
                .ToArray() ?? [],
            iresource.ResourceKind,
            iresource is IEntityResource er
                ? er.Views.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                : [],
            ((IResourceInternal) iresource)
            .GetInnerResources()
            .Select(r => Make(r, trace))
            .ToArray()
        );
    }

    /// <inheritdoc />
    /// <summary>
    ///     Returns all the resources that are declared within a given namespace
    /// </summary>
    [RESTableView]
    public class InNamespace : ISelector<AvailableResource>
    {
        /// <summary>
        ///     The namespace to match against
        /// </summary>
        public string? Namespace { get; }

        /// <inheritdoc />
        public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
        {
            var @namespace = request.Conditions.Pop(nameof(Namespace), Operators.EQUALS)?.Value as string;
            if (@namespace is not null)
                if (!@namespace.EndsWith("."))
                    @namespace += ".";
            if (@namespace is null)
                return request.Context.Client.AccessRights.Keys
                    .Where(r => r is { IsGlobal: true, IsInnerResource: false })
                    .OrderBy(r => r.Name)
                    .Select(r => Make(r, request));
            return request.Context.Client.AccessRights.Keys
                .Where(r => r is { IsGlobal: true, IsInnerResource: false })
                .Where(r => r.Name.StartsWith(@namespace, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Name)
                .Select(r => Make(r, request));
        }
    }
}
