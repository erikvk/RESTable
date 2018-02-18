using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Results.Error.BadRequest;

namespace RESTar.Admin
{
    /// <summary>
    /// A meta-resource that provides representations of all resources in a RESTar instance
    /// </summary>
    [RESTar(Description = description)]
    internal sealed class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
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
        public Methods[] EnabledMethods { get; set; }

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

        [JsonConstructor]
        public Resource() => Provider = "undefined";

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

        internal static Resource Make(IResource iresource)
        {
            var entityResource = iresource as IEntityResource;
            return new Resource
            {
                Name = iresource.Name,
                Alias = iresource.Alias,
                Description = iresource.Description ?? "No description",
                EnabledMethods = iresource.AvailableMethods.ToArray(),
                Editable = entityResource?.Editable == true,
                IsInternal = iresource.IsInternal,
                Type = iresource.Type.FullName,
                Views = entityResource != null
                    ? (entityResource.Views?.Select(v => new ViewInfo(v.Name, v.Description ?? "No description")).ToArray()
                       ?? new ViewInfo[0])
                    : null,
                IResource = iresource,
                Provider = entityResource?.Provider ?? "Terminal",
                Kind = entityResource != null ? ResourceKind.EntityResource : ResourceKind.TerminalResource,
                InnerResources = ((IResourceInternal) iresource).InnerResources?.Select(Make).ToArray()
            };
        }

        /// <inheritdoc />
        public int Insert(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var entity in request.GetEntities())
            {
                if (string.IsNullOrWhiteSpace(entity.Name))
                    throw new Exception("Missing or invalid name for new resource");
                entity.Provider = "DynamicResource";
                entity.ResolveDynamicResourceName();
                if (!string.IsNullOrWhiteSpace(entity.Alias) && ResourceAlias.Exists(entity.Alias, out var alias))
                    throw new AliasAlreadyInUse(alias);
                if (entity.EnabledMethods?.Any() != true)
                    entity.EnabledMethods = RESTarConfig.Methods;
                DynamicResource.MakeTable(entity);
                count += 1;
            }
            return count;
        }

        private void ResolveDynamicResourceName()
        {
            switch (Name)
            {
                case var _ when !Regex.IsMatch(Name, RegEx.DynamicResourceName):
                    throw new Exception($"Resource name '{Name}' contains invalid characters: Only letters, nu" +
                                        "mbers and underscores are valid in resource names. Dots can be used " +
                                        "to organize resources into namespaces. No other characters can be used.");
                case var _ when Name.StartsWith(".") || Name.Contains("..") || Name.EndsWith("."):
                    throw new Exception($"'{Name}' is not a valid resource name: Invalid namespace syntax");
            }
            if (!Name.StartsWith("RESTar.Dynamic."))
            {
                if (Name.StartsWith("restar.dynamic.", StringComparison.OrdinalIgnoreCase))
                    Name = $"RESTar.Dynamic.{Name.Split(new[] {'.'}, 3).Last()}";
                else Name = $"RESTar.Dynamic.{Name}";
            }
            if (RESTarConfig.ResourceByName.ContainsKey(Name))
                throw new Exception($"Invalid resource name '{Name}'. Name already in use.");
        }

        /// <inheritdoc />
        public int Update(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in request.GetEntities())
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

                if (iresource is IEntityResource er && er.Editable)
                {
                    #region Edit other properties (available for dynamic resources)

                    var dynamicResource = DynamicResource.Get(iresource.Name);
                    var diresource = (IResourceInternal) iresource;

                    if (iresource.Description != resource.Description)
                    {
                        diresource.Description = resource.Description;
                        dynamicResource.Description = resource.Description;
                        updated = true;
                    }

                    var methods = resource.EnabledMethods?.Distinct().ToList();
                    methods?.Sort(MethodComparer.Instance);
                    if (methods != null && !iresource.AvailableMethods.SequenceEqual(methods))
                    {
                        diresource.AvailableMethods = methods;
                        dynamicResource.AvailableMethods = methods;
                        updated = true;
                    }

                    if (resource.Name != iresource.Name)
                    {
                        resource.ResolveDynamicResourceName();
                        dynamicResource.Name = resource.Name;
                        var alias = ResourceAlias.GetByResource(iresource.Name);
                        if (alias != null) alias._resource = resource.Name;
                        RESTarConfig.RemoveResource(iresource);
                        ResourceFactory.MakeDynamicResource(dynamicResource);
                        updated = true;
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
            return request.GetEntities().Count(DynamicResource.DeleteTable);
        }
    }
}