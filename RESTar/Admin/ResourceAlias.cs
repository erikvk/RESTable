using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Starcounter;
using static RESTar.Methods;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Admin
{
    /// <summary>
    /// The ResourceAlias resource is used to assign an alias to a resource, making 
    /// it possible to reference the resource with only the alias.
    /// </summary>
    [Database, RESTar(GET, DELETE, Description = description)]
    public class ResourceAlias
    {
        private const string description = "The ResourceAlias resource is used to assign an " +
                                           "alias to a resource, making it possible to reference " +
                                           "the resource with only the alias.";

        /// <summary>
        /// The alias string
        /// </summary>
        public string Alias;

        internal string _resource;

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
                    var r = RESTarConfig.ResourceByName[value.ToLower()];
                    _resource = r.Name;
                }
                catch (KeyNotFoundException)
                {
                    this.Delete();
                    throw new UnknownResourceForAliasException(value, RESTar.Resource.Find(value));
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
        [IgnoreDataMember]
        public IResource IResource => RESTarConfig.ResourceByName[Resource.ToLower()];

        private const string AliasSQL = "SELECT t FROM RESTar.Admin.ResourceAlias t WHERE t.Alias =?";
        private const string ResourceSQL = "SELECT t FROM RESTar.Admin.ResourceAlias t WHERE t.Resource =?";
        private const string AllSQL = "SELECT t FROM RESTar.Admin.ResourceAlias t";

        internal static IEnumerable<ResourceAlias> All => Db.SQL<ResourceAlias>(AllSQL);

        /// <summary>
        /// Gets a ResourceAlias by its alias (case insensitive)
        /// </summary>
        public static ResourceAlias ByAlias(string alias) => Db.SQL<ResourceAlias>(AliasSQL, alias).First;

        /// <summary>
        /// Gets a ResourceAlias by its resource name
        /// </summary>
        public static ResourceAlias ByResource(string resourceName) => Db
            .SQL<ResourceAlias>(ResourceSQL, resourceName).First;

        /// <summary>
        /// Returns true if and only if there is an alias with this name
        /// </summary>
        public static bool Exists(string alias, out ResourceAlias resourceAlias)
        {
            resourceAlias = ByAlias(alias);
            return resourceAlias != null;
        }

        /// <summary>
        /// Returns true if and only if there is an alias for the given resource type
        /// </summary>
        public static bool Exists(Type type, out ResourceAlias alias)
        {
            alias = ByResource(type.FullName);
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