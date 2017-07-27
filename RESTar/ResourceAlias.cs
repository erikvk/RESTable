using System;
using System.Collections.Generic;
using Starcounter;
using static RESTar.RESTarPresets;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    internal static class ResourceAlias<T> where T : class
    {
        private const string SQL = "SELECT t FROM RESTar.ResourceAlias t WHERE t.Resource =?";
        public static ResourceAlias Get => Db.SQL<ResourceAlias>(SQL, typeof(T).FullName).First;
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
                    throw new UnknownResourceForAliasException(value, value.FindResource());
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
        public IResource IResource => RESTarConfig.ResourceByName[Resource.ToLower()];

        private const string AliasSQL = "SELECT t FROM RESTar.ResourceAlias t WHERE t.Alias =?";
        private const string ResourceSQL = "SELECT t FROM RESTar.ResourceAlias t WHERE t.Resource =?";

        /// <summary>
        /// Gets a ResourceAlias by its alias
        /// </summary>
        public static ResourceAlias ByAlias(string alias) => Db.SQL<ResourceAlias>(AliasSQL, alias).First;

        /// <summary>
        /// Gets a ResourceAlias by its resource
        /// </summary>
        public static ResourceAlias ByResource(Type type) => Db.SQL<ResourceAlias>(ResourceSQL, type?.FullName).First;

        /// <summary>
        /// Returns true if and only if there is an alias with this name
        /// </summary>
        public static bool Exists(string alias, out ResourceAlias resourceAlias)
        {
            resourceAlias = ByAlias(alias);
            return resourceAlias != null;
        }

        /// <summary>
        /// Returns true if and only if there is no such alias
        /// </summary>
        public static bool NotExists(string alias) => !Exists(alias, out var _);

        /// <summary>
        /// Returns true if and only if there is an alias for the given resource type
        /// </summary>
        public static bool Exists(Type type, out ResourceAlias alias)
        {
            alias = ByResource(type);
            return alias != null;
        }

        /// <summary>
        /// Returns true if and only if there is no alias for the given resource type
        /// </summary>
        public static bool NotExists(Type resource) => !Exists(resource, out var _);

        /// <summary>
        /// Returns true if and only if there is an alias for the given resource
        /// </summary>
        public static bool Exists(IResource resource, out ResourceAlias alias) => Exists(resource.Type, out alias);

        /// <summary>
        /// Returns true if and only if there is no alias for the given resource
        /// </summary>
        public static bool NotExists(IResource resource) => !Exists(resource, out var _);

        /// <summary>
        /// </summary>
        public override string ToString() => Alias;
    }
}