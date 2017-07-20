using System;
using System.Collections.Generic;
using RESTar.Internal;
using Starcounter;
using static RESTar.RESTarPresets;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    internal static class ResourceAlias<T> where T : class
    {
        public static string Get => DB.Get<ResourceAlias>("Resource", typeof(T).FullName)?.Alias;
    }

    /// <summary>
    /// The ResourceAlias resource is used to assign an alias to a resource, making 
    /// it possible to reference the resource with only the alias. 
    /// </summary>
    [Database, RESTar(ReadAndWrite)]
    public class ResourceAlias
    {
        /// <summary>
        /// The alias string
        /// </summary>
        public string Alias;

        private string _resource;

        /// <summary>
        /// The name of the resource to bind the alias to
        /// </summary>
        public string Resource
        {
            get => _resource;
            set
            {
                try
                {
                    if (value.StartsWith("RESTar.DynamicResource"))
                    {
                        _resource = DynamitControl.GetByTableNameLower(value.ToLower()).FullName;
                        return;
                    }
                    var r = RESTarConfig.ResourceByName[value.ToLower()];
                    _resource = r.Name;
                }
                catch (KeyNotFoundException)
                {
                    this.Delete();
                    var match = value.FindResource();
                    throw new UnknownResourceForAliasException(value, match.Type);
                }
                catch
                {
                    this.Delete();
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the resource denoted by this alias
        /// </summary>
        public IResource GetResource() => RESTarConfig.ResourceByName[Resource.ToLower()];

        /// <summary>
        /// Gets a resource by its alias
        /// </summary>
        public static IResource ByAlias(string alias) => DB.Get<ResourceAlias>("Alias", alias)?.GetResource();

        /// <summary>
        /// Returns true if and only if there is an alias for the given resource type
        /// </summary>
        public static bool Exists(Type resource) => DB.Exists<ResourceAlias>("Resource", resource.FullName);

        /// <summary>
        /// Returns true if and only if there is no alias for the given resource type
        /// </summary>
        public static bool NotExists(Type resource) => !Exists(resource);

        /// <summary>
        /// Returns true if and only if there is an alias for the given resource
        /// </summary>
        public static bool Exists(IResource resource) => DB.Exists<ResourceAlias>("Resource", resource.Name);

        /// <summary>
        /// Returns true if and only if there is no alias for the given resource
        /// </summary>
        public static bool NotExists(IResource resource) => !Exists(resource);
    }
}