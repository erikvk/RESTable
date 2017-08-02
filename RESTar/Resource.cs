using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Operations;
using Starcounter;
using static System.Reflection.BindingFlags;
using static RESTar.RESTarConfig;
using static RESTar.RESTarPresets;
using IResource = RESTar.Internal.IResource;

namespace RESTar
{
    /// <summary>
    /// A resource that lists all available resources in a RESTar instance
    /// </summary>
    [RESTar(ReadAndWrite)]
    public sealed class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
    {
        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public RESTarMethods[] AvailableMethods { get; set; }

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Is this resource editable?
        /// </summary>
        public bool Editable { get; private set; }

        /// <summary>
        /// The alias of this resource, if any
        /// </summary>
        public string Alias { get; set; }

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

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Resource> Select(IRequest<Resource> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var conditions = request.Conditions.Redirect<IResource>(direct: "Type", to: "Type.FullName");
            return Resources.Where(conditions).Where(r => r.IsGlobal).Select(m => new Resource
            {
                Name = m.Name,
                Alias = m.Alias,
                AvailableMethods = m.AvailableMethods.ToArray(),
                Editable = m.Editable,
                IsInternal = m.IsInternal,
                Type = m.Type.FullName,
                IResource = m,
                ResourceType = m.ResourceType
            });
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
                if (string.IsNullOrEmpty(entity.Alias))
                    throw new Exception("No Alias for new resource");
                if (ResourceAlias.Exists(entity.Alias, out var alias))
                    throw new AliasAlreadyInUseException(alias);
                entity.AvailableMethods = Methods;
                DynamicResource.MakeTable(entity);
                count += 1;
            }
            return count;
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
                var updated = false;
                var iresource = resource.IResource;
                if (!string.IsNullOrWhiteSpace(resource.Alias) && resource.Alias != iresource.Alias)
                {
                    iresource.Alias = resource.Alias;
                    updated = true;
                }
                if (iresource.Editable)
                {
                    var methods = resource.AvailableMethods?.Distinct().ToList();
                    methods?.Sort(MethodComparer.Instance);
                    if (methods != null && !iresource.AvailableMethods.SequenceEqual(methods))
                    {
                        iresource.AvailableMethods = methods;
                        var dynamicResource = resource.GetDynamicResource();
                        if (dynamicResource != null)
                            dynamicResource.AvailableMethods = methods;
                        updated = true;
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
            var count = 0;
            foreach (var resource in entities)
            {
                DynamicResource.DeleteTable(resource);
                count += 1;
            }
            return count;
        }

        private static readonly MethodInfo AUTO_MAKER = typeof(Resource)
            .GetMethod(nameof(AUTO_MAKE), NonPublic | Static);

        private static readonly MethodInfo DYNAMIC_AUTO_MAKER = typeof(Resource)
            .GetMethod(nameof(DYNAMIC_AUTO_MAKE), NonPublic | Static);

        internal static void AutoMakeDynamicResource(DynamicResource resource) => DYNAMIC_AUTO_MAKER
            .MakeGenericMethod(resource.Table)
            .Invoke(null, new object[] {resource.Attribute});

        internal static void AutoMakeResource(Type type) => AUTO_MAKER
            .MakeGenericMethod(type)
            .Invoke(null, null);

        private static void AUTO_MAKE<T>() where T : class =>
            Resource<T>.Make(typeof(T).GetAttribute<RESTarAttribute>());

        private static void DYNAMIC_AUTO_MAKE<T>(RESTarAttribute attribute) where T : class =>
            Resource<T>.Make(attribute);

        private const string DynamicResourceSQL = "SELECT t FROM RESTar.Internal.DynamicResource t WHERE t.Name =?";
        internal DynamicResource GetDynamicResource() => Db.SQL<DynamicResource>(DynamicResourceSQL, Name).First;

        /// <summary>
        /// Registers a new resource with the RESTar instance
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="preset">The preset to configure available methods from</param>
        /// <param name="addMethods">Additional methods, apart from the ones defined by the preset</param>
        public static void Register<T>(RESTarPresets preset, params RESTarMethods[] addMethods) where T : class
        {
            var methods = preset.ToMethods().Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Register<T>(methods[0], methods.Length > 1 ? methods.Skip(1).ToArray() : null);
        }

        /// <summary>
        /// Registers a new resource with the RESTar instance
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="method">A method to make available for this resource</param>
        /// <param name="addMethods">Additional methods to make available</param>
        public static void Register<T>(RESTarMethods method, params RESTarMethods[] addMethods) where T : class
        {
            var methods = new[] {method}.Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Register<T>(methods[0], methods.Length > 1 ? methods.Skip(1).ToArray() : null, null);
        }

        /// <summary>
        /// Registers a new resource with the RESTar instance
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="preset">The preset to configure available methods from</param>
        /// <param name="addMethods">Additional methods, apart from the ones defined by the preset</param>
        /// <param name="selector">The selector to use for this resource</param>
        /// <param name="inserter">The inserter to use for this resource</param>
        /// <param name="updater">The updater to use for this resource</param>
        /// <param name="deleter">The deleter to use for this resource</param>
        public static void Register<T>
        (
            RESTarPresets preset,
            IEnumerable<RESTarMethods> addMethods = null,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null
        ) where T : class
        {
            var methods = preset.ToMethods().Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Register
            (
                method: methods[0],
                addMethods: methods.Length > 1 ? methods.Skip(1).ToArray() : null,
                selector: selector,
                inserter: inserter,
                updater: updater,
                deleter: deleter
            );
        }

        /// <summary>
        /// Registers a new resource with the RESTar instance
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="method">A method to make available for this resource</param>
        /// <param name="addMethods">Additional methods to make available</param>
        /// <param name="selector">The selector to use for this resource</param>
        /// <param name="inserter">The inserter to use for this resource</param>
        /// <param name="updater">The updater to use for this resource</param>
        /// <param name="deleter">The deleter to use for this resource</param>
        public static void Register<T>
        (
            RESTarMethods method,
            IEnumerable<RESTarMethods> addMethods = null,
            Selector<T> selector = null,
            Inserter<T> inserter = null,
            Updater<T> updater = null,
            Deleter<T> deleter = null
        ) where T : class
        {
            if (typeof(T).HasAttribute<RESTarAttribute>())
                throw new InvalidOperationException("Cannot manually register resources that have a RESTar " +
                                                    "attribute. Resources decorated with a RESTar attribute " +
                                                    "are registered automatically");
            var attribute = new RESTarAttribute(method, addMethods?.ToArray());
            Resource<T>.Make(attribute, selector, inserter, updater, deleter);
        }

        /// <summary>
        /// Finds a resource by a search string, can be a partial resource name. If no resource 
        /// is found, throws an UnknownResourceException. If more than one resource is found, throws
        /// an AmbiguousResourceException.
        /// </summary>
        public static IResource Find(string searchString)
        {
            searchString = searchString.ToLower();
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return resource;
            var matches = ResourceByName
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

        internal static ICollection<IResource> FindMany(string searchString)
        {
            searchString = searchString.ToLower();
            var asterisks = searchString.Count(i => i == '*');
            if (asterisks > 1)
                throw new Exception("Invalid resource string syntax");
            if (asterisks == 1)
            {
                if (searchString.Last() != '*')
                    throw new Exception("Invalid resource string syntax");
                var commonPart = searchString.Split('*')[0];
                var matches = ResourceByName
                    .Where(pair => pair.Key.StartsWith(commonPart))
                    .Select(pair => pair.Value)
                    .Union(ResourceAlias.All
                        .Where(alias => alias.Alias.StartsWith(commonPart))
                        .Select(alias => alias.IResource))
                    .ToList();
                if (matches.Any()) return matches;
                throw new UnknownResourceException(searchString);
            }
            var resource = ResourceAlias.ByAlias(searchString)?.IResource;
            if (resource == null)
                ResourceByName.TryGetValue(searchString, out resource);
            if (resource != null)
                return new[] {resource};
            throw new UnknownResourceException(searchString);
        }

        /// <summary>
        /// Finds a resource by name (case sensitive) and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource Get(string name) => ResourceByName.SafeGet(name)
                                                    ?? throw new UnknownResourceException(name);

        /// <summary>
        /// Finds a resource by name (case insensitive) and returns null
        /// if no resource is found
        /// </summary>
        public static IResource SafeGet(string name) => ResourceByName.SafeGetNoCase(name);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource Get(Type type) => ResourceByType.SafeGet(type)
                                                  ?? throw new UnknownResourceException(type.FullName);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource SafeGet(Type type) => ResourceByType.SafeGet(type);

        /// <summary>
        /// Finds a resource by target type and throws an UnknownResourceException
        /// if no resource is found.
        /// </summary>
        public static IResource<T> Get<T>() where T : class => Resource<T>.Get;

        /// <summary>
        /// Finds a resource by target type, and returns null if no
        /// resource is found.
        /// </summary>
        public static IResource<T> SafeGet<T>() where T : class => Resource<T>.SafeGet;
    }
}