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
    /// A resource that provides representations of all resources in a RESTar instance
    /// </summary>
    [RESTar]
    internal sealed class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
    {
        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; set; }

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

        [JsonConstructor]
        public Resource(RESTarResourceType resourceType)
        {
            ResourceType = resourceType;
        }

        public Resource()
        {
        }

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
                if (string.IsNullOrWhiteSpace(entity.Name))
                    throw new Exception("Missing or invalid name for new resource");
                entity.ResourceType = DynamicStarcounter;
                entity.ResolveDynamicResourceName();
                if (!string.IsNullOrWhiteSpace(entity.Alias) && ResourceAlias.Exists(entity.Alias, out var alias))
                    throw new AliasAlreadyInUseException(alias);
                if (entity.AvailableMethods?.Any() != true)
                    entity.AvailableMethods = RESTarConfig.Methods;
                DynamicResource.MakeTable(entity);
                count += 1;
            }
            return count;
        }

        private const string CharacterRegex = @"^[a-zA-Z0-9_\.]+$";
        private const string OnlyUnderscores = @"^_+$";

        private void ResolveDynamicResourceName()
        {
            switch (Name)
            {
                case var _ when !Regex.IsMatch(Name, CharacterRegex):
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
                    #region Edit available methods (available for all editable resources)

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

                    #endregion

                    if (resource.ResourceType == DynamicStarcounter)
                    {
                        #region Edit resource name (available for all editable dynamic starcounter resources

                        if (resource.Name != iresource.Name)
                        {
                            var dynamicResource = RESTar.Resource.GetDynamicResource(iresource.Name);
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