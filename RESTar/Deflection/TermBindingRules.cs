namespace RESTar.Deflection
{
    /// <summary>
    /// The rule used for binding terms to a resource
    /// </summary>
    public enum TermBindingRules
    {
        /// <summary>
        /// First finds static properties, then creates dynamic properties
        /// for any unknown property
        /// </summary>
        StaticWithDynamicFallback,

        /// <summary>
        /// First finds dynamic properties, then (at runtime) searches for 
        /// static properties if no dynamic property was found
        /// </summary>
        DynamicWithStaticFallback,

        /// <summary>
        /// Binds only static properties, and throws an exception if no 
        /// static property was found
        /// </summary>
        OnlyStatic
    }
}