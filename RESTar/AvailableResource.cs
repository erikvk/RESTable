using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;

namespace RESTar
{
    /// <summary>
    /// Gets the available resources for the current user
    /// </summary>
    [RESTar(Methods.GET)]
    internal sealed class AvailableResource : ISelector<AvailableResource>
    {
        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The alias of this resource, if any
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Methods[] AvailableMethods { get; set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
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
                    AvailableMethods = accessRights.SafeGet(resource)?.Intersect(resource.AvailableMethods).ToArray()
                                       ?? new Methods[0]
                })
                .Where(request.Conditions);
        }
    }
}