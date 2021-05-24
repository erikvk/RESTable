using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Admin
{
    /// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
    /// <inheritdoc cref="IAsyncUpdater{T}" />
    /// <summary>
    /// A meta-resource that provides representations of all resources in a RESTable instance
    /// </summary>
    [RESTable(Method.GET, Description = description)]
    public class Resource : ISelector<Resource>
    {
        private const string description = "A meta-resource that provides representations " +
                                           "of all resources in a RESTable instance.";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Resource descriptions are visible in the AvailableMethods resource
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public Method[] EnabledMethods { get; set; }

        /// <summary>
        /// Is this resource declared, as opposed to procedural?
        /// </summary>
        public bool IsDeclared { get; internal set; }

        /// <summary>
        /// Is this resource procedural, as opposed to declared?
        /// </summary>
        [RESTableMember(name: "IsProcedural")]
        public bool _IsProcedural => !IsDeclared;

        /// <summary>
        /// Is this resource internal?
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// The type targeted by this resource.
        /// </summary>
        public Type Type { get; internal set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [RESTableMember(hideIfNull: true)]
        public ViewInfo[] Views { get; private set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [RESTableMember(hide: true)]
        public IResource IResource { get; private set; }

        /// <summary>
        /// The resource provider that generated this resource
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// The resource type, entity resource or terminal resource
        /// </summary>
        public ResourceKind Kind { get; set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [RESTableMember(hideIfNull: true)]
        public IEnumerable<Resource> InnerResources { get; private set; }

        internal static T Make<T>(IResource iresource) where T : Resource, new()
        {
            if (iresource is null) return null;
            var entityResource = iresource as IEntityResource;
            return new T
            {
                Name = iresource.Name,
                Description = iresource.Description ?? "No description",
                EnabledMethods = iresource.AvailableMethods.ToArray(),
                IsInternal = iresource.IsInternal,
                IsDeclared = entityResource?.IsDeclared ?? true,
                Type = iresource.Type,
                Views = entityResource is not null
                    ? entityResource.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                      ?? new ViewInfo[0]
                    : null,
                IResource = iresource,
                Provider = entityResource?.Provider ?? (iresource is IBinaryResource ? "Binary" : "Terminal"),
                Kind = iresource.ResourceKind,
                InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(Make<T>).ToArray()
            };
        }

        /// <inheritdoc />
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            return request
                .GetRequiredService<ResourceCollection>()
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(Make<Resource>);
        }
    }
}