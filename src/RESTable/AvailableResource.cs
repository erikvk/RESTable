using System;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Linq;
using static RESTable.Method;

namespace RESTable
{
    /// <inheritdoc />
    /// <summary>
    /// A resource that generates a list of the available resources for the current user
    /// </summary>
    [RESTable(GET, Description = description, GETAvailableToAll = true)]
    public sealed class AvailableResource : ISelector<AvailableResource>
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
        /// The methods that have been enabled for this resource
        /// </summary>
        public Method[] Methods { get; set; }

        /// <summary>
        /// The resource type, entity resource or terminal resource
        /// </summary>
        public ResourceKind Kind { get; set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [RESTableMember(hideIfNull: true)] public ViewInfo[] Views { get; private set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [RESTableMember(hideIfNull: true)] public AvailableResource[] InnerResources { get; private set; }

        /// <inheritdoc />
        public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return request.Context.Client.AccessRights.Keys?
                .Where(r => r.IsGlobal && !r.IsInnerResource)
                .OrderBy(r => r.Name)
                .Select(r => Make(r, request));
        }

        /// <inheritdoc />
        /// <summary>
        /// Returns all the resources that are declared within a given namespace
        /// </summary>
        [RESTableView]
        public class InNamespace : ISelector<AvailableResource>
        {
            /// <summary>
            /// The namespace to match against
            /// </summary>
            public string Namespace { get; set; }

            /// <inheritdoc />
            public IEnumerable<AvailableResource> Select(IRequest<AvailableResource> request)
            {
                var @namespace = request.Conditions.Pop(nameof(Namespace), Operators.EQUALS).Value as string;
                if (@namespace != null)
                    if (!@namespace.EndsWith("."))
                        @namespace = @namespace + ".";
                if (@namespace == null)
                    return request.Context.Client.AccessRights.Keys?
                        .Where(r => r.IsGlobal && !r.IsInnerResource)
                        .OrderBy(r => r.Name)
                        .Select(r => Make(r, request));
                return request.Context.Client.AccessRights.Keys?
                    .Where(r => r.IsGlobal && !r.IsInnerResource)
                    .Where(r => r.Name.StartsWith(@namespace, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r.Name)
                    .Select(r => Make(r, request));
            }
        }

        internal static AvailableResource Make(IResource iresource, ITraceable trace) => new AvailableResource
        {
            Name = iresource.Name,
            Description = iresource.Description ?? "No description",
            Methods = trace.Context.Client.AccessRights.SafeGet(iresource)?
                          .Intersect(iresource.AvailableMethods)
                          .ToArray() ?? new Method[0],
            Kind = iresource.ResourceKind,
            Views = iresource is IEntityResource er
                ? er.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                  ?? new ViewInfo[0]
                : null,
            InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(r => Make(r, trace)).ToArray()
        };
    }
}