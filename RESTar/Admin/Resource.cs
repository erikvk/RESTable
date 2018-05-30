using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RESTar.Internal.Sc;
using RESTar.Linq;
using RESTar.Meta;
using RESTar.Meta.Internal;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using static RESTar.Internal.EntityResourceProviderController;

namespace RESTar.Admin
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// A meta-resource that provides representations of all resources in a RESTar instance
    /// </summary>
    [RESTar(Description = description)]
    public class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
    {
        private const string description = "A meta-resource that provides representations " +
                                           "of all resources in a RESTar instance.";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; set; }

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
        public Method[] EnabledMethods { get; set; }

        /// <summary>
        /// Is this resource declared, as opposed to procedural?
        /// </summary>
        public bool IsDeclared { get; internal set; }

        /// <summary>
        /// Is this resource procedural, as opposed to declared?
        /// </summary>
        [RESTarMember(name: "IsProcedural")] public bool _IsProcedural => !IsDeclared;

        /// <summary>
        /// Is this resource internal?
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// The type targeted by this resource.
        /// </summary>
        public string Type { get; internal set; }

        /// <summary>
        /// The views for this resource
        /// </summary>
        [RESTarMember(hideIfNull: true)] public ViewInfo[] Views { get; private set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [RESTarMember(hide: true)] public IResource IResource { get; private set; }

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
        [RESTarMember(hideIfNull: true)] public Resource[] InnerResources { get; private set; }

        private bool IsProcedural(out IProceduralEntityResource proceduralResource, out IEntityResource entityResource,
            out EntityResourceProvider provider)
        {
            proceduralResource = null;
            entityResource = null;
            provider = null;

            if (IResource is IEntityResource _entityResource)
                entityResource = _entityResource;
            else return false;
            if (!EntityResourceProviders.TryGetValue(entityResource.Provider, out provider) || !provider.SupportsProceduralResources)
                return false;
            var resource = entityResource;
            if (provider._Select().FirstOrDefault(r => r.Name == resource.Name) is IProceduralEntityResource _dynamicResource)
                proceduralResource = _dynamicResource;
            else return false;
            return true;
        }

        internal static Resource Make(IResource iresource)
        {
            if (iresource == null) return null;
            var entityResource = iresource as IEntityResource;
            return new Resource
            {
                Name = iresource.Name,
                Alias = iresource.Alias,
                Description = iresource.Description ?? "No description",
                EnabledMethods = iresource.AvailableMethods.ToArray(),
                IsInternal = iresource.IsInternal,
                IsDeclared = entityResource?.IsDeclared ?? true,
                Type = iresource.Type.RESTarTypeName(),
                Views = entityResource != null
                    ? entityResource.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                      ?? new ViewInfo[0]
                    : null,
                IResource = iresource,
                Provider = entityResource?.Provider ?? (iresource is IBinaryResource ? "Binary" : "Terminal"),
                Kind = iresource.ResourceKind,
                InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(Make).ToArray()
            };
        }

        /// <inheritdoc />
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return RESTarConfig.Resources
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(Make)
                .Where(request.Conditions);
        }

        /// <inheritdoc />
        public int Insert(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in request.GetInputEntities())
            {

                count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Update(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in request.GetInputEntities())
            {
                #region Edit alias (available for all resources)

                var updated = false;
                var iresource = resource.IResource;
                if (resource.Alias != iresource.Alias)
                {
                    var iresourceInternal = (IResourceInternal) iresource;
                    iresourceInternal.SetAlias(resource.Alias);
                    updated = true;
                }

                #endregion

                if (resource.IsProcedural(out var dynamicResource, out var entityResource, out var dynamicProvider))
                {
                    #region Edit other properties (available for dynamic resources)

                    var resourceInternal = (IResourceInternal) entityResource;

                    bool updater()
                    {
                        if (entityResource.Description != resource.Description)
                        {
                            dynamicResource.Description = resource.Description;
                            updated = true;
                        }
                        var methods = resource.EnabledMethods?.ResolveMethodsCollection();
                        if (methods != null && !iresource.AvailableMethods.SequenceEqual(methods))
                        {
                            dynamicResource.Methods = methods.ToArray();
                            updated = true;
                        }
                        return updated;
                    }

                    if (dynamicProvider.Update(dynamicResource, updater))
                    {
                        resourceInternal.Description = dynamicResource.Description;
                        var methods = dynamicResource.Methods.ResolveMethodsCollection();
                        resourceInternal.AvailableMethods = methods;
                    }

                    #endregion
                }
                if (updated) count += 1;
            }
            return count;
        }

        /// <inheritdoc />
        public int Delete(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return request.GetInputEntities().Count(item =>
            {
                var type = item.IResource.Type;
                if (item.IsProcedural(out var dr, out _, out var dp))
                {
                    var entityResourceProvider = (EntityResourceProvider) dp;
                    entityResourceProvider.;
                    return true;
                }
                return false;
            });
        }
    }
}