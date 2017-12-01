using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Linq;
using static Newtonsoft.Json.NullValueHandling;
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
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The alias of this resource, if any
        /// </summary>
        [JsonProperty(NullValueHandling = Ignore)] public string Alias { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Methods[] Methods { get; set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [JsonProperty(NullValueHandling = Ignore)] public object Views { get; private set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [JsonProperty(NullValueHandling = Ignore)] public AvailableResource[] InnerResources { get; private set; }

        /// <inheritdoc />
        public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var rights = RESTarConfig.AuthTokens[request.AuthToken];

            AvailableResource Make(IResource iresource) => new AvailableResource
            {
                Name = iresource.Name,
                Alias = iresource.Alias,
                Description = iresource.Description ?? "No description",
                Methods = rights.SafeGet(iresource)?
                              .Intersect(iresource.AvailableMethods)
                              .ToArray() ?? new Methods[0],
                Views = iresource.Views?.Select(v => new
                {
                    v.Name,
                    Description = v.Description ?? "No description"
                }).ToArray() ?? new object[0],
                InnerResources = ((IResourceInternal) iresource).InnerResources?
                    .Select(Make)
                    .ToArray()
            };

            return rights?.Keys
                .Where(r => r.IsGlobal && !r.IsInnerResource)
                .OrderBy(r => r.Name)
                .Select(Make)
                .Where(request.Conditions);
        }
    }
}