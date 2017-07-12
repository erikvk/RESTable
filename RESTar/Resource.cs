using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using RESTar.Internal;
using RESTar.Operations;
using static System.Reflection.BindingFlags;
using static RESTar.Internal.DynamicResource;
using static RESTar.RESTarConfig;
using static RESTar.RESTarPresets;

namespace RESTar
{
    /// <summary>
    /// A resource that lists all available resources in a RESTar instance
    /// </summary>
    [RESTar(ReadAndWrite)]
    public sealed class Resource : ISelector<Resource>, IInserter<Resource>, IUpdater<Resource>, IDeleter<Resource>
    {
        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Is this resource editable?
        /// </summary>
        public bool Editable { get; private set; }

        /// <summary>
        /// Is this resource visible in the view?
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// The methods that have been enabled for this resource
        /// </summary>
        public RESTarMethods[] AvailableMethods { get; set; }

        /// <summary>
        /// The alias of this resource, if any
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// The string name of the target type. Is shown as "TargetType"
        /// in JSON.
        /// </summary>
        [DataMember(Name = "TargetType")]
        public string TargetTypeString => TargetType?.FullName;

        /// <summary>
        /// The type targeted by this resource.
        /// </summary>
        [IgnoreDataMember]
        public Type TargetType { get; set; }

        /// <summary>
        /// The IResource of this resource
        /// </summary>
        [IgnoreDataMember]
        public IResource IResource { get; private set; }

        /// <summary>
        /// The resource type
        /// </summary>
        public RESTarResourceType ResourceType { get; set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<Resource> Select(IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return Resources
                .Filter(request.Conditions)
                .Select(m => new Resource
                {
                    Name = m.Name,
                    Alias = m.Alias,
                    AvailableMethods = m.AvailableMethods,
                    Editable = m.Editable,
                    TargetType = m.TargetType,
                    IResource = m,
                    ResourceType = m.ResourceType
                }).ToList();
        }

        /// <summary>
        /// </summary>
        public int Insert(IEnumerable<Resource> resources, IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            var dynamicTables = resources.ToList();
            foreach (var entity in dynamicTables)
            {
                if (string.IsNullOrEmpty(entity.Alias))
                    throw new Exception("No Alias for new resource");
                if (DB.Exists<ResourceAlias>("Alias", entity.Alias))
                    throw new Exception($"Invalid Alias: '{entity.Alias}' is used to refer to another resource");
                entity.AvailableMethods = Methods;
                MakeTable(entity);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// </summary>
        public int Update(IEnumerable<Resource> entities, IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                DeleteTable(resource);
                MakeTable(resource);
                count += 1;
            }
            return count;
        }

        /// <summary>
        /// </summary>
        public int Delete(IEnumerable<Resource> entities, IRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var count = 0;
            foreach (var resource in entities)
            {
                DeleteTable(resource);
                count += 1;
            }
            return count;
        }

        private static readonly MethodInfo AUTO_MAKER = typeof(Resource)
            .GetMethod(nameof(AUTO_MAKE), NonPublic | Static);

        internal static void AutoMakeResource(Type type) => AUTO_MAKER
            .MakeGenericMethod(type)
            .Invoke(null, null);

        private static void AUTO_MAKE<T>() where T : class =>
            Resource<T>.Make(typeof(T).GetAttribute<RESTarAttribute>());

        /// <summary>
        /// Registers a new resource with the RESTar instance
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        /// <param name="preset">The preset to configure available methods from</param>
        /// <param name="addMethods">Additional methods, apart from the ones defined by the preset</param>
        public static void Register<T>(RESTarPresets preset, params RESTarMethods[] addMethods) where T : class
        {
            var methods = preset.ToMethods().Union(addMethods ?? new RESTarMethods[0]).ToArray();
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null);
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
            Register<T>(methods.First(), methods.Length > 1 ? methods.Skip(1).ToArray() : null, null);
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
                method: methods.First(),
                addMethods: methods.Length > 1
                    ? methods.Skip(1).ToArray()
                    : null,
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
        /// Finds a resource by name (case insensitive)
        /// </summary>
        public static IResource Find(string name) => ResourceByName.SafeGetNoCase(name);

        /// <summary>
        /// Finds a resource by target type
        /// </summary>
        public static IResource Get(Type type) => ResourceByType.SafeGet(type);

        internal static IResource<T> Get<T>() where T : class => ResourceByType.SafeGet(typeof(T)) as IResource<T>;

        internal static IResource<Resource> MetaResource => ResourceByType[typeof(Resource)] as IResource<Resource>;
    }
}