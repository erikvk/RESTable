using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Auth;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Methods;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Gets the available resources for the current user
    /// </summary>
    [RESTar(GET, Description = description, GETAvailableToAll = true)]
    public sealed class AvailableResource : ISelector<AvailableResource>
    {
        private const string description = "The AvailableResource resource contains all resources " +
                                           "available for the current user, as defined by the access " +
                                           "rights assigned to its API key. It is the default resource " +
                                           "used when no resource is specified in the request URI.";

        internal static IEntityResource<AvailableResource> Resource = EntityResource<AvailableResource>.Get;

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
        [RESTarMember(hideIfNull: true)] public string Alias { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Methods[] Methods { get; set; }

        /// <summary>
        /// The resource type, entity resource or terminal resource
        /// </summary>
        public ResourceKind Kind { get; set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public ViewInfo[] Views { get; private set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public AvailableResource[] InnerResources { get; private set; }

        /// <inheritdoc />
        public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var _rights = Authenticator.AuthTokens.SafeGet(request.Context.Client.AuthToken);
            return _rights?.Keys
                .Where(r => r.IsGlobal && !r.IsInnerResource)
                .OrderBy(r => r.Name)
                .Select(r => Make(r, _rights))
                .Where(request.Conditions);
        }

        private static AvailableResource Make(IResource iresource, AccessRights rights) => new AvailableResource
        {
            Name = iresource.Name,
            Alias = iresource.Alias,
            Description = iresource.Description ?? "No description",
            Methods = rights.SafeGet(iresource)?
                          .Intersect(iresource.AvailableMethods)
                          .ToArray() ?? new Methods[0],
            Kind = iresource is IEntityResource ? ResourceKind.EntityResource : ResourceKind.TerminalResource,
            Views = iresource is IEntityResource er
                ? er.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                  ?? new ViewInfo[0]
                : null,
            InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(r => Make(r, rights)).ToArray()
        };
    }
}