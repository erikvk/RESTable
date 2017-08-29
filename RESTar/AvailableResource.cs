using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using static RESTar.Methods;

namespace RESTar
{
    /// <summary>
    /// Gets the available resources for the current user
    /// </summary>
    [RESTar(GET, Description = description)]
    internal sealed class AvailableResource : ISelector<AvailableResource>
    {
        private const string description = "The AvailableResource resource contains all resources " +
                                           "available for the current user, as defined by the access " +
                                           "rights assigned to its API key. It is the default resource " +
                                           "used when no resource is specified in the request URI.";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The alias of this resource, if any
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Methods[] Methods { get; set; }

        /// <inheritdoc />
        public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var accessRights = RESTarConfig.AuthTokens[request.AuthToken];
            return accessRights.Keys
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(resource => new AvailableResource
                {
                    Name = resource.Name,
                    Alias = resource.Alias,
                    Description = resource.Description ?? "No description",
                    Methods = accessRights.SafeGet(resource)?.Intersect(resource.AvailableMethods).ToArray()
                              ?? new Methods[0]
                })
                .Where(request.Conditions);
        }
    }
}