using System;
using RESTable.Results;

namespace RESTable.Meta
{
    /// <summary>
    /// A static class for manually getting RESTable entity resources by type
    /// </summary>
    public static class EntityResource
    {
        /// <summary>
        /// Gets the entity resource for a given type, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource Get(Type type) => RESTableConfig.ResourceByType.SafeGet(type) as IEntityResource
                                                        ?? throw new UnknownResource(type.GetRESTableTypeName());

        /// <summary>
        /// Gets the entity resource for a given type or returns null if there is no such resource
        /// </summary>
        public static IEntityResource SafeGet(Type type) => RESTableConfig.ResourceByType.SafeGet(type) as IEntityResource;

        /// <summary>
        /// Gets the entity resource with the given name, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource Get(string name) => RESTableConfig.ResourceByName.SafeGet(name) as IEntityResource
                                                          ?? throw new UnknownResource(name);

        /// <summary>
        /// Gets the entity resource with the given name or returns null if there is no such resource
        /// </summary>
        public static IEntityResource SafeGet(string name) => RESTableConfig.ResourceByName.SafeGet(name) as IEntityResource;
    }

    /// <summary>
    /// A static generic class for manually getting RESTable entity resources by type
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    public static class EntityResource<T> where T : class
    {
        /// <summary>
        /// Gets the entity resource for a given type, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource<T> Get => RESTableConfig.ResourceByType.SafeGet(typeof(T)) as IEntityResource<T>
                                                ?? throw new UnknownResource(typeof(T).GetRESTableTypeName());

        /// <summary>
        /// Gets the entity resource for a given type or null if there is no such resource
        /// </summary>
        public static IEntityResource<T> SafeGet => RESTableConfig.ResourceByType.SafeGet(typeof(T)) as IEntityResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given EntityResource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }
}