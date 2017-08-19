using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Internal.RESTarResourceType;
using IResource = RESTar.Internal.IResource;

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
        /// The IResource of this resource
        /// </summary>
        [IgnoreDataMember]
        public IResource IResource { get; private set; }

        /// <summary>
        /// The resource type
        /// </summary>
        public RESTarResourceType ResourceType { get; private set; }

        [JsonConstructor]
        public Resource(RESTarResourceType resourceType) => ResourceType = resourceType;

        private Resource() => ResourceType = undefined;

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
                    Description = resource.Description ?? "No description",
                    EnabledMethods = resource.AvailableMethods.ToArray(),
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
                if (string.IsNullOrWhiteSpace(entity.Name))
                    throw new Exception("Missing or invalid name for new resource");
                entity.ResourceType = DynamicStarcounter;
                entity.ResolveDynamicResourceName();
                if (!string.IsNullOrWhiteSpace(entity.Alias) && ResourceAlias.Exists(entity.Alias, out var alias))
                    throw new AliasAlreadyInUseException(alias);
                if (entity.EnabledMethods?.Any() != true)
                    entity.EnabledMethods = RESTarConfig.Methods;
                DynamicResource.MakeTable(entity);
                count += 1;
            }
            return count;
        }

        private const string AllowedCharacters = @"^[a-zA-Z0-9_\.]+$";
        private const string OnlyUnderscores = @"^_+$";

        private void ResolveDynamicResourceName()
        {
            switch (Name)
            {
                case var _ when !Regex.IsMatch(Name, AllowedCharacters):
                    throw new Exception($"Resource name '{Name}' contains invalid characters: Only letters, nu" +
                                        "mbers and underscores are valid in resource names. Dots can be used " +
                                        "to organize resources into namespaces. No other characters can be used.");
                case var _ when Name.StartsWith(".") || Name.Contains("..") || Name.EndsWith("."):
                    throw new Exception($"'{Name}' is not a valid resource name: Invalid namespace syntax");
            }
            if (!Name.StartsWith("RESTar.Dynamic."))
            {
                if (Name.ToLower().StartsWith("restar.dynamic."))
                    Name = $"RESTar.Dynamic.{Name.Split(new[] {'.'}, 3).Last()}";
                else Name = $"RESTar.Dynamic.{Name}";
            }
            if (RESTarConfig.ResourceByName.ContainsKey(Name.ToLower()))
                throw new Exception($"Invalid resource name '{Name}'. Name already in use.");
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
                #region Edit alias (available for all resources)

                var updated = false;
                var iresource = resource.IResource;
                if (resource.Alias != iresource.Alias)
                {
                    iresource.Alias = resource.Alias;
                    updated = true;
                }

                #endregion

                if (iresource.Editable)
                {
                    #region Edit other properties (available for dynamic resources)

                    var dynamicResource = DynamicResource.Get(iresource.Name);
                    dynamic diresource = iresource;

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
                        var alias = ResourceAlias.ByResource(iresource.Name);
                        if (alias != null) alias._resource = resource.Name;
                        RESTarConfig.RemoveResource(iresource);
                        RESTar.Resource.RegisterDynamicResource(dynamicResource);
                        updated = true;
                    }

                    #endregion
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
            return entities.Count(DynamicResource.DeleteTable);
        }
    }
}