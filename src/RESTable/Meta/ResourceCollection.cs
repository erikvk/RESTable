using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTable.Auth;
using RESTable.Linq;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.Meta
{
    public class ResourceCollection : ICollection<IResource>
    {
        private IDictionary<string, IResource?> ResourceFinder { get; }
        private IDictionary<string, IResource> ResourceByName { get; }
        private IDictionary<Type, IResource> ResourceByType { get; }
        private RESTableConfigurator Configurator { get; set; }
        private TypeCache TypeCache { get; set; }
        private RootAccess RootAccess { get; set; }

        public ResourceCollection()
        {
            ResourceFinder = new ConcurrentDictionary<string, IResource?>(StringComparer.OrdinalIgnoreCase);
            ResourceByName = new Dictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
            ResourceByType = new Dictionary<Type, IResource>();

            // Resolved in SetDependencies to avoid circular dependencies
            Configurator = null!;
            TypeCache = null!;
            RootAccess = null!;
        }

        internal void SetDependencies
        (
            RESTableConfigurator configurator,
            TypeCache typeCache,
            RootAccess rootAccess
        )
        {
            Configurator = configurator;
            TypeCache = typeCache;
            RootAccess = rootAccess;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<IResource> GetEnumerator() => ResourceByName.Values.GetEnumerator();

        #region ICollection

        public void Add(IResource item) => AddResource(item);

        public void Clear()
        {
            ResourceFinder.Clear();
            ResourceByName.Clear();
            ResourceByType.Clear();
        }

        public bool Contains(IResource item) => ResourceByName.Values.Contains(item);
        public void CopyTo(IResource[] array, int arrayIndex) => ResourceByName.Values.CopyTo(array, arrayIndex);
        public bool Remove(IResource item) => RemoveResource(item);
        public int Count => ResourceByName.Count;
        public bool IsReadOnly => false;

        #endregion

        internal void AddResource(IResource toAdd)
        {
            ResourceByName[toAdd.Name] = toAdd;
            ResourceByType[toAdd.Type] = toAdd;
            AddToResourceFinder(toAdd);
            TypeCache.GetDeclaredProperties(toAdd.Type);
            if (Configurator.IsConfigured)
                RootAccess.Load();
        }

        internal bool RemoveResource(IResource toRemove)
        {
            var r = ResourceByName.Remove(toRemove.Name);
            ResourceByType.Remove(toRemove.Type);
            ReloadResourceFinder();
            RootAccess.Load();
            return r;
        }

        private void ReloadResourceFinder()
        {
            ResourceFinder.Clear();
            foreach (var resource in ResourceByName.Values)
                AddToResourceFinder(resource);
        }

        private void AddToResourceFinder(IResource toAdd)
        {
            string[] makeResourceParts(IResource resource)
            {
                switch (resource)
                {
                    case var _ when resource.IsInternal: return new[] {resource.Name};
                    case var _ when resource.IsInnerResource:
                        var dots = resource.Name.Count('.');
                        return resource.Name.Split(new[] {'.'}, dots);
                    default: return resource.Name.Split('.');
                }
            }

            var parts = makeResourceParts(toAdd);

            for (var i = 0; i < parts.Length; i += 1)
            {
                var key = string.Join(".", parts.Skip(i));
                if (ResourceFinder.ContainsKey(key))
                    ResourceFinder[key] = null;
                else ResourceFinder[key] = toAdd;
            }
        }

        #region Get API

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name. If no resource 
        /// is found, throws an UnknownResource exception. If more than one resource is found, throws
        /// an AmbiguousResource exception.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <param name="kind">The resource kind to filter results against</param>
        /// </summary>
        public IResource FindResource(string searchString, ResourceKind kind = ResourceKind.All)
        {
            if (searchString is null) throw new UnknownResource("null");
            if (!ResourceFinder.TryGetValue(searchString, out var resource))
                throw new UnknownResource(searchString);
            if (resource is null)
                throw new AmbiguousResource(searchString);
            if (!kind.HasFlag(resource.ResourceKind))
                throw new WrongResourceKind(searchString, kind, resource.ResourceKind);
            return resource;
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <param name="resource">The found resource (if any)</param>
        /// <param name="error">Describes the error that occured when locating the resource (if any)</param>
        /// <param name="kind">The resource kind to filter results against</param>
        /// </summary>
        public bool TryFindResource(string searchString, out IResource resource, out Error error, ResourceKind kind = ResourceKind.All)
        {
            searchString = searchString.ToLower();
            error = null;
            if (!ResourceFinder.TryGetValue(searchString, out resource))
            {
                error = new UnknownResource(searchString);
                return false;
            }
            if (resource is null)
            {
                error = new AmbiguousResource(searchString);
                return false;
            }
            if (!kind.HasFlag(resource.ResourceKind))
            {
                error = new WrongResourceKind(searchString, kind, resource.ResourceKind);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Finds a resource by a search string. The string can be a partial resource name.
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <param name="resource">The found resource (if any)</param>
        /// <param name="error">Describes the error that occured when locating the resource (if any)</param>
        /// </summary>
        public bool TryFindResource<T>(string searchString, out T resource, out Error error) where T : IResource
        {
            var kind = typeof(T).GetResourceKind();
            if (!TryFindResource(searchString, out var _resource, out error, kind))
            {
                resource = default;
                return false;
            }
            resource = (T) _resource;
            return true;
        }

        /// <summary>
        /// Finds a number of resources based on a search string. To include more than one resource in 
        /// the search, use the wildcard character (asterisk '*'). For example: To find all resources in a namespace
        /// 'MyApplication.Utilities', use the search string "myapplication.utilities.*" or any case 
        /// variant of it.
        /// </summary>
        /// <param name="searchString">The case insensitive string to use for the search</param>
        /// <returns></returns>
        public IResource[] SafeFindResources(string searchString)
        {
            switch (searchString.Count(i => i == '*'))
            {
                case 0:
                    if (TryFindResource(searchString, out var resource, out _))
                        return new[] {resource};
                    return new IResource[0];
                case 1 when searchString.Last() != '*':
                    throw new Exception("Invalid resource string syntax. The asterisk must be the last character");
                case 1:
                    var commonPart = searchString.TrimEnd('*');
                    var commonPartDots = commonPart.Count(c => c == '.');
                    return ResourceByName
                        .Where(pair => pair.Key.StartsWith(commonPart, StringComparison.OrdinalIgnoreCase) &&
                                       pair.Key.Count(c => c == '.') == commonPartDots)
                        .Select(pair => pair.Value)
                        .ToArray();
                default: throw new Exception("Invalid resource string syntax. Can only include one asterisk (*)");
            }
        }

        /// <summary>
        /// Tries to retrieve the resource with the given name
        /// </summary>
        public bool TryGetResource(string name, out IResource resource) => ResourceByName.TryGetValue(name, out resource);

        /// <summary>
        /// Tries to retrieve the resource with the given type
        /// </summary>
        public bool TryGetResource(Type type, out IResource resource) => ResourceByType.TryGetValue(type, out resource);

        /// Gets the resource for a given type, or throws an UnknownResource exception if there is no such resource
        /// </summary>
        public IResource GetResource(Type type) => ResourceByType.SafeGet(type) ?? throw new UnknownResource(type.GetRESTableTypeName());

        /// <summary>
        /// Gets the resource for a given type or returns null if there is no such resource
        /// </summary>
        public IResource? SafeGetResource(Type type) => ResourceByType.SafeGet(type);

        /// <summary>
        /// Finds a resource by name (case insensitive) and throws an UnknownResource exception
        /// if no resource is found.
        /// </summary>
        public IResource GetResource(string name) => ResourceByName.SafeGet(name) ?? throw new UnknownResource(name);

        /// <summary>
        /// Finds a resource by name (case insensitive) and returns null
        /// if no resource is found
        /// </summary>
        public IResource SafeGetResource(string name) => ResourceByName.SafeGet(name);

        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public IResource<T> GetResource<T>() where T : class => ResourceByType.SafeGet(typeof(T)) as IResource<T>
                                                                ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());

        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public IResource<T>? SafeGetResource<T>() where T : class => ResourceByType.SafeGet(typeof(T)) as IResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given resource
        /// </summary>
        public string GetResourceSpecifier<T>() where T : class => GetResource<T>().Name;

        /// <summary>
        /// Gets the terminal resource for a given type, and throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public ITerminalResource<T> GetTerminalResource<T>() where T : Terminal => ResourceByType.SafeGet(typeof(T)) as ITerminalResource<T>
                                                                                   ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());


        /// <summary>
        /// Gets the terminal resource for a given type or null if there is no such resource
        /// </summary>
        public ITerminalResource<T>? SafeGetTerminalResource<T>() where T : Terminal => ResourceByType.SafeGet(typeof(T)) as ITerminalResource<T>;

        #endregion
    }
}