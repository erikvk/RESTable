using RESTar.Results;

namespace RESTar.Meta
{
    /// <summary>
    /// A static generic class for manually getting RESTar entity resources by type
    /// </summary>
    /// <typeparam name="T">The type to register</typeparam>
    public static class EntityResource<T> where T : class
    {
        /// <summary>
        /// Gets the entity resource for a given type, or throws an UnknownResource exception 
        /// if there is no such resource
        /// </summary>
        public static IEntityResource<T> Get => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IEntityResource<T>
                                                ?? throw new UnknownResource(typeof(T).RESTarTypeName());

        /// <summary>
        /// Gets the entity resource for a given type or null if there is no such resource
        /// </summary>
        public static IEntityResource<T> SafeGet => RESTarConfig.ResourceByType.SafeGet(typeof(T)) as IEntityResource<T>;

        /// <summary>
        /// Gets the resource specifier for a given EntityResource
        /// </summary>
        public static string ResourceSpecifier => Get.Name;
    }
}