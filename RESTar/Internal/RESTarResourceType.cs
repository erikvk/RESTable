namespace RESTar.Internal
{
    /// <summary>
    /// The resource types used internally by RESTar
    /// </summary>
    public enum RESTarResourceType
    {
        /// <summary>
        /// Created by user, non-persistent - should be initialized. Regular
        /// Starcounter resources.
        /// </summary>
        ScStatic,

        /// <summary>
        /// Created by user, non-persistent - should not be initialized.
        /// Resources that inherit from DDictionary.
        /// </summary>
        ScDynamic,

        /// <summary>
        /// Created by user, non-persistent - no public fields
        /// </summary>
        Virtual,

        /// <summary>
        /// Created by RESTar, DynamicResource 01-64. Persistent resources.
        /// </summary>
        Dynamic
    }
}