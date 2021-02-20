using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.MetadataLevel;

namespace RESTable.OData
{
    /// <inheritdoc />
    /// <summary>
    /// This resource lists all the available resources of the OData service.
    /// </summary>
    [RESTable(Method.GET, GETAvailableToAll = true, Description = description)]
    public class ServiceDocument : ISelector<ServiceDocument>
    {
        private const string description = "The OData metadata document listing the resources of this application";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string name { get; private set; }

        /// <summary>
        /// The resource kind, for example "EntitySet"
        /// </summary>
        public string kind { get; private set; }

        /// <summary>
        /// The URL to the resource, for example "User"
        /// </summary>
        public string url { get; private set; }

        /// <inheritdoc />
        public IEnumerable<ServiceDocument> Select(IRequest<ServiceDocument> request) => Metadata
            .Get(OnlyResources)
            .EntityResources
            .Select(resource => new ServiceDocument
            {
                kind = "EntitySet",
                name = resource.Name,
                url = resource.Name
            }); 
    }
}