using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RESTar.Admin;
using RESTar.Internal;
using RESTar.Operations;
using Starcounter;
using IResource = RESTar.Internal.IResource;

namespace RESTar
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
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, throws an UnknownResourceException. If more than one resource is found, throws
        /// an AmbiguousResourceException.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource Find(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                RESTarConfig.ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var matches = RESTarConfig.ResourceByName
                .Where(pair => pair.Value.IsGlobal && pair.Key.EndsWith($".{searchString}"))
                .Select(pair => pair.Value)
                .ToList();
            switch (matches.Count)
            {
                case 0: throw new UnknownResourceException(searchString);
                case 1: return matches[0];
                default: throw new AmbiguousResourceException(searchString, matches.Select(c => c.Name).ToList());
            }
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, returns null. If more than one resource is found, throws an AmbiguousResourceException.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource SafeFind(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                RESTarConfig.ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var matches = RESTarConfig.ResourceByName
                .Where(pair => pair.Value.IsGlobal && pair.Key.EndsWith($".{searchString}"))
                .Select(pair => pair.Value)
                .ToList();
            switch (matches.Count)
            {
                case 0: return null;
                case 1: return matches[0];
                default: throw new AmbiguousResourceException(searchString, matches.Select(c => c.Name).ToList());
            }
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
            searchString = searchString.ToLower();
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
                    var matches = RESTarConfig.ResourceByName
                        .Where(pair => pair.Key.StartsWith(commonPart) &&
                                       pair.Key.Count(c => c == '.') == commonPartDots)
                        .Select(pair => pair.Value)
                        .ToArray();
                    return matches;
                default: throw new Exception("Invalid resource string syntax. Can only include one asterisk (*)");
            }
        }

        /// <summary>
        /// Finds a resource by name (case sensitive) and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource Get(string name) => RESTarConfig.ResourceByName.SafeGet(name)
                                                    ?? throw new UnknownResourceException(name);

        /// <summary>
        /// Finds a resource by name (case insensitive) and returns null
        /// if no resource is found
        /// </summary>
        public static IResource SafeGet(string name) => RESTarConfig.ResourceByName.SafeGetNoCase(name);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource Get(Type type) => RESTarConfig.ResourceByType.SafeGet(type)
                                                  ?? throw new UnknownResourceException(type.FullName);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource SafeGet(Type type) => RESTarConfig.ResourceByType.SafeGet(type);

        #endregion

        #region Helpers

        private static readonly MethodInfo AUTO_MAKER;
        private static readonly MethodInfo DYNAMIC_AUTO_MAKER;
        private const string DynamicResourceSQL = "SELECT t FROM RESTar.Internal.DynamicResource t WHERE t.Name =?";

        internal static DynamicResource GetDynamicResource(string name) => Db
            .SQL<DynamicResource>(DynamicResourceSQL, name).First;

        static Resource()
        {
            DYNAMIC_AUTO_MAKER = typeof(Resource).GetMethod(nameof(DYNAMIC_AUTO_MAKE),
                BindingFlags.NonPublic | BindingFlags.Static);
            AUTO_MAKER = typeof(Resource).GetMethod(nameof(AUTO_MAKE), BindingFlags.NonPublic | BindingFlags.Static);
        }

        internal static void AutoMakeDynamicResource(DynamicResource resource)
        {
            DYNAMIC_AUTO_MAKER.MakeGenericMethod(resource.Table).Invoke(null, new object[] {resource.Attribute});
        }

        internal static void AutoMakeResource(Type type)
        {
            AUTO_MAKER.MakeGenericMethod(type).Invoke(null, null);
        }

        private static void AUTO_MAKE<T>() where T : class
        {
            Internal.Resource<T>.Make(RESTarAttribute<T>.Get);
        }

        private static void DYNAMIC_AUTO_MAKE<T>(RESTarAttribute attribute) where T : class
        {
            Internal.Resource<T>.Make(attribute);
        }

        #endregion
    }

    /// <summary>
    /// A static generic class for manually registering types as RESTar resources
    /// and finding resources by type
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    public static class Resource<T> where T : class
    {
        /// <summary>
        /// Registers a class as a RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        public static void Register(params Methods[] methods)
        {
            if (!methods.Any()) methods = RESTarConfig.Methods;
            Register(methods.OrderBy(i => i, MethodComparer.Instance).ToArray(), null);
        }

        /// <summary>
        /// Registers a class as a RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        /// <param name="methods">The methods to make available for this resource</param>
        /// <param name="selector">The selector to use for this resource</param>
        /// <param name="inserter">The inserter to use for this resource</param>
        /// <param name="updater">The updater to use for this resource</param>
        /// <param name="deleter">The deleter to use for this resource</param>
        /// <param name="singleton">Is this a singleton resource?</param>
        /// <param name="internalResource">Is this an internal resource?</param>
        public static void Register
        (
            ICollection<Methods> methods,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null,
            bool singleton = false,
            bool internalResource = false)
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var attribute = internalResource
                ? new RESTarInternalAttribute(methods.ToArray())
                : new RESTarAttribute(methods.ToArray());
            attribute.Singleton = singleton;
            Internal.Resource<T>.Make(attribute, selector, inserter, updater, deleter);
        }

        /// <summary>
        /// Gets the resource for a given type, or throws an UnknownResourceException 
        /// if there is no such resource
        /// </summary>
        public static IResource<T> Get => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IResource<T> ??
                                          throw new UnknownResourceException(typeof(T).FullName);

        /// <summary>
        /// Gets the resource for a given type or null if there is no such resource
        /// </summary>
        public static IResource<T> SafeGet => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IResource<T>;
    }
}