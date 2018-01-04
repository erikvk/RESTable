using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Resources;
using RESTar.Results.Fail.BadRequest;
using static Newtonsoft.Json.NullValueHandling;
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
        /// The views for this resource
        /// </summary>
        [JsonProperty(NullValueHandling = Ignore)]
        public object Views { get; private set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [IgnoreDataMember] public IResource IResource { get; private set; }

        /// <summary>
        /// The resource provider that generated this resource
        /// </summary>
        public string Provider { get; private set; }

        /// <summary>
        /// Inner resources for this resource
        /// </summary>
        [JsonProperty(NullValueHandling = Ignore)]
        public Resource[] InnerResources { get; private set; }

        [JsonConstructor]
        public Resource() => Provider = "undefined";

        /// <inheritdoc />
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            Resource Make(IResource iresource) => new Resource
            {
                Name = iresource.Name,
                Alias = iresource.Alias,
                Description = iresource.Description ?? "No description",
                EnabledMethods = iresource.AvailableMethods.ToArray(),
                Editable = iresource.Editable,
                IsInternal = iresource.IsInternal,
                Type = iresource.Type.FullName,
                Views = iresource.Views?.Select(v => new
                {
                    v.Name,
                    Description = v.Description ?? "No description"
                }).ToArray() ?? new object[0],
                IResource = iresource,
                Provider = iresource.Provider,
                InnerResources = ((IResourceInternal) iresource).InnerResources?
                    .Select(Make)
                    .ToArray()
            };

            return RESTarConfig.Resources
                .Where(r => r.IsGlobal)
                .OrderBy(r => r.Name)
                .Select(Make)
                .Where(request.Conditions);
        }

        /// <inheritdoc />
        public int Insert(IEnumerable<Resource> resources, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var entity in resources)
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
        public int Delete(IEnumerable<Resource> entities, IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return entities.Count(DynamicResource.DeleteTable);
        }
    }
}