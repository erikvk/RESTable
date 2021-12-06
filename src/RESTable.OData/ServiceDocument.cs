using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.MetadataLevel;

namespace RESTable.OData;

/// <inheritdoc />
/// <summary>
///     This resource lists all the available resources of the OData service.
/// </summary>
[RESTable(Method.GET, GETAvailableToAll = true, Description = description)]
public class ServiceDocument : ISelector<ServiceDocument>
{
    private const string description = "The OData metadata document listing the resources of this application";

    public ServiceDocument(string name, string kind, string url)
    {
        this.name = name;
        this.kind = kind;
        this.url = url;
    }

    /// <summary>
    ///     The name of the resource
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     The resource kind, for example "EntitySet"
    /// </summary>
    public string kind { get; }

    /// <summary>
    ///     The URL to the resource, for example "User"
    /// </summary>
    public string url { get; }

    /// <inheritdoc />
    public IEnumerable<ServiceDocument> Select(IRequest<ServiceDocument> request)
    {
        return Metadata
            .GetMetadata
            (
                OnlyResources,
                request.GetRequiredService<RootAccess>(),
                request.GetRequiredService<TypeCache>()
            )
            .EntityResources
            .Select(resource => new ServiceDocument(resource.Name, "EntitySet", resource.Name));
    }
}