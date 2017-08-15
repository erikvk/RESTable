using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            var resource = Admin.ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource != null) return resource;
            if (!RESTarConfig.ResourceFinder.TryGetValue(searchString, out resource))
                throw new UnknownResourceException(searchString);
            if (resource == null)
                throw new AmbiguousResourceException(searchString);
            return resource;
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, returns null. If more than one resource is found, throws an AmbiguousResourceException.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// </summary>
        public static IResource SafeFind(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = Admin.ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource != null) return resource;
            if (!RESTarConfig.ResourceFinder.TryGetValue(searchString, out resource))
                return null;
            if (resource == null)
                throw new AmbiguousResourceException(searchString);
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

        internal static void RegisterDynamicResource(DynamicResource resource)
        {
            DYNAMIC_AUTO_MAKER.MakeGenericMethod(resource.Table).Invoke(null, new object[] {resource});
        }

        internal static void AutoRegister(Type type)
        {
            AUTO_MAKER.MakeGenericMethod(type).Invoke(null, null);
        }

        private static void AUTO_MAKE<T>() where T : class
        {
            Internal.Resource<T>.Make(typeof(T).FullName, RESTarAttribute<T>.Get);
        }

        private static void DYNAMIC_AUTO_MAKE<T>(DynamicResource resource) where T : class
        {
            Internal.Resource<T>.Make(resource.Name, resource.Attribute);
        }

        #endregion
    }

    /// <summary>
    /// A static generic class for manually registering types as RESTar resources
    /// and finding static resources by type
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    public static class Resource<T> where T : class
    {
        /// <summary>
        /// Registers a class as a static RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        public static void Register(params Methods[] methods)
        {
            if (!methods.Any()) methods = RESTarConfig.Methods;
            Register(methods.OrderBy(i => i, MethodComparer.Instance).ToArray(), null);
        }

        /// <summary>
        /// Registers a class as a static RESTar resource. If no methods are provided in the 
        /// methods list, all methods will be enabled for this resource.
        /// </summary>
        public static void Register(string description, params Methods[] methods)
        {
            if (!methods.Any()) methods = RESTarConfig.Methods;
            Register(methods.OrderBy(i => i, MethodComparer.Instance).ToArray(), description: description);
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
        /// <param name="description">A description for the resource</param>
        public static void Register
        (
            ICollection<Methods> methods,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null,
            bool singleton = false,
            bool internalResource = false,
            string description = null)
        {
            if (methods?.Any() != true) methods = RESTarConfig.Methods;
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            if (SafeGet != null)
                throw new InvalidOperationException($"A resource with type '{typeof(T).FullName}' already exists");
            if (typeof(T).Assembly == typeof(Resource).Assembly)
                throw new InvalidOperationException("Cannot register a class in the RESTar assembly as resource");
            var attribute = internalResource
                ? new RESTarInternalAttribute(methods.ToArray())
                : new RESTarAttribute(methods.ToArray());
            attribute.Singleton = singleton;
            attribute.Description = description;
            Internal.Resource<T>.Make(typeof(T).FullName, attribute, selector, inserter, updater, deleter);
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