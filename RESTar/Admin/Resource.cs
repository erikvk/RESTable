using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using RESTar.Internal;
using RESTar.Linq;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Admin
{
    /// <summary>
    /// A resource that provides representations of all resources in a RESTar instance
    /// </summary>
    [RESTar]
    internal sealed class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
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
        /// Is this resource editable?
        /// </summary>
        public bool Editable { get; private set; }

        /// <summary>
        /// Is this resource internal?
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>
        /// The type targeted by this resource.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [IgnoreDataMember]
        public IResource IResource { get; private set; }

        /// <summary>
        /// The resource type
        /// </summary>
        public RESTarResourceType ResourceType { get; private set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return RESTarConfig.Resources
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(resource => new Resource
                {
                    Name = resource.Name,
                    Alias = resource.Alias,
                    AvailableMethods = resource.AvailableMethods.ToArray(),
                    Editable = resource.Editable,
                    IsInternal = resource.IsInternal,
                    Type = resource.Type.FullName,
                    IResource = resource,
                    ResourceType = resource.ResourceType,
                })
                .Where(request.Conditions);
        }

        /// <summary>
        /// RESTar inserter (don't use)
        /// </summary>
        public int Insert(IEnumerable<Resource> resources, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var entity in resources)
            {
                if (string.IsNullOrEmpty(entity.Alias))
                    throw new Exception("No Alias for new resource");
                if (RESTarConfig.Resources.Any(r => r.Name.EqualsNoCase(entity.Alias)))
                    throw new AliasEqualToResourceNameException(entity.Alias);
                if (ResourceAlias.Exists(entity.Alias, out var alias))
                    throw new AliasAlreadyInUseException(alias);
                if (entity.AvailableMethods?.Any() != true)
                    entity.AvailableMethods = RESTarConfig.Methods;
                DynamicResource.MakeTable(entity);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// RESTar updater (don't use)
        /// </summary>
        public int Update(IEnumerable<Resource> entities, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                var updated = false;
                var iresource = resource.IResource;
                if (!string.IsNullOrWhiteSpace(resource.Alias) && resource.Alias != iresource.Alias)
                {
                    iresource.Alias = resource.Alias;
                    updated = true;
                }
                if (iresource.Editable)
                {
                    var methods = resource.AvailableMethods?.Distinct().ToList();
                    methods?.Sort(MethodComparer.Instance);
                    if (methods != null && !iresource.AvailableMethods.SequenceEqual(methods))
                    {
                        dynamic r = iresource;
                        r.AvailableMethods = methods;
                        var dynamicResource = RESTar.Resource.GetDynamicResource(resource.Name);
                        if (dynamicResource != null)
                            dynamicResource.AvailableMethods = methods;
                        updated = true;
                    }
                }
                if (updated) count += 1;
            }
            return count;
        }

        /// <summary>
        /// RESTar deleter (don't use)
        /// </summary>
        public int Delete(IEnumerable<Resource> entities, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                DynamicResource.DeleteTable(resource);
                count += 1;
            }
            return count;
        }

    }
}