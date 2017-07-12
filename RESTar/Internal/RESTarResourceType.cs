namespace RESTar.Internal
{
    /// <summary>
    /// The resource types used internally by RESTar
    /// </summary>
    public enum RESTarResourceType
    {
        /// <summary>
        /// Regular Starcounter resources. Resource created at runtime
        /// </summary>
        StaticStarcounter,

        /// <summary>
        /// Resource that inherit from DDictionary. Resource created at runtime
        /// </summary>
        DynamicStarcounter,

        /// <summary>
        /// Non-starcounter resource. Members known at compile time. Resource 
        /// created at runtime
        /// </summary>
        StaticVirtual,

        /// <summary>
        /// Non-starcounter resource. Members not known at compile time. Resource 
        /// created at runtime
        /// </summary>
        DynamicVirtual,

        /// <summary>
        /// Created by RESTar, DynamicResource 01-64. Resources persist
        /// </summary>
        RESTarDynamicResource,
    }
}