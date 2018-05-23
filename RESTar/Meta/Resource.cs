﻿using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Resources;
using RESTar.Results;

namespace RESTar.Meta
{
    /// <summary>
    /// A static class for finding registered RESTar resources
    /// </summary>
    public static class Resource
    {
        #region Find and get resources

        /// <summary>
        /// Gets all registered RESTar resources
        /// </summary>
        public static IEnumerable<IResource> All => RESTarConfig.Resources;

        /// <summary>
        /// Gets all registered RESTar resources that were claimed by the given 
        /// ResourceProvider type.
        /// </summary>
        public static IEnumerable<IEntityResource> ClaimedBy<T>() where T : EntityResourceProvider => All
            .OfType<IEntityResource>()
            .Where(r => r.ClaimedBy<T>());

        /// <summary>
        /// Gets all registered RESTar resources that were claimed by the given 
        /// ResourceProvider type.
        /// </summary>
        public static ICollection<IEntityResource> ClaimedBy(EntityResourceProvider provider) => All
            .OfType<IEntityResource>()
            .Where(r => r.Provider == provider.GetProviderId())
            .ToList();

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, throws an UnknownResource exception. If more than one resource is found, throws
        /// an AmbiguousResource exception.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource Find(string searchString)
        {
            if (searchString == null) throw new UnknownResource("null");
            var resource = Admin.ResourceAlias.GetByAlias(searchString)?.IResource;
            if (resource != null) return resource;
            if (!RESTarConfig.ResourceFinder.TryGetValue(searchString, out resource))
                throw new UnknownResource(searchString);
            if (resource == null)
                throw new AmbiguousResource(searchString);
            return resource;
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, returns null. If more than one resource is found, throws an AmbiguousResource exception.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource SafeFind(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = Admin.ResourceAlias.GetByAlias(searchString)?.IResource;
            if (resource != null) return resource;
            if (!RESTarConfig.ResourceFinder.TryGetValue(searchString, out resource))
                return null;
            if (resource == null)
                throw new AmbiguousResource(searchString);
            return resource;
        }

        /// <summary>
        /// Finds a number of resources based on a search string. To include more than one resource in 
        /// the search, use the wildcard character (asterisk '*'). For example: To find all resources in a namespace
        /// 'MyApplication.Utilities', use the search string "myapplication.utilities.*" or any case 
        /// variant of it.
        /// </summary>
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <returns></returns>
        public static IResource[] SafeFindMany(string searchString)
        {
            switch (searchString.Count(i => i == '*'))
            {
                case 0:
                    var found = SafeFind(searchString);
                    if (found == null) return new IResource[0];
                    return new[] {found};
                case 1 when searchString.Last() != '*':
                    throw new Exception("Invalid resource string syntax. The asterisk must be the last character");
                case 1:
                    var commonPart = searchString.TrimEnd('*');
                    var commonPartDots = commonPart.Count(c => c == '.');
                    return RESTarConfig.ResourceByName
                        .Where(pair => pair.Key.StartsWith(commonPart, StringComparison.OrdinalIgnoreCase) &&
                                       pair.Key.Count(c => c == '.') == commonPartDots)
                        .Select(pair => pair.Value)
                        .ToArray();
                default: throw new Exception("Invalid resource string syntax. Can only include one asterisk (*)");
            }
        }

        /// <summary>
        /// Gets the entity resource for a given type, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IResource Get(Type type) => RESTarConfig.ResourceByType.SafeGet(type) ?? throw new UnknownResource(type.RESTarTypeName());

        /// <summary>
        /// Gets the entity resource for a given type or returns null if there is no such resource
        /// </summary>
        public static IResource SafeGet(Type type) => RESTarConfig.ResourceByType.SafeGet(type);

        /// <summary>
        /// Finds a resource by name (case insensitive) and throws an UnknownResource exception
        /// if no resource is found.
        /// </summary>
        public static IResource Get(string name) => RESTarConfig.ResourceByName.SafeGet(name) ?? throw new UnknownResource(name);

        /// <summary>
        /// Finds a resource by name (case insensitive) and returns null
        /// if no resource is found
        /// </summary>
        public static IResource SafeGet(string name) => RESTarConfig.ResourceByName.SafeGet(name);

        #endregion
    }

    /// <summary>
    /// A static generic class for manually getting RESTar resources by type
    /// </summary>
    public static class Resource<T> where T : class
    {
        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IResource<T> Get => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IResource<T>
                                          ?? throw new UnknownResource(typeof(T).RESTarTypeName());


        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public static IResource<T> SafeGet => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given terminal resource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }
}