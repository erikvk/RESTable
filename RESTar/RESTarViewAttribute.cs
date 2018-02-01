using System;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Registers an inner class as a RESTar view for the outer Resource type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarViewAttribute : Attribute
    {
        /// <summary>
        /// If true, unknown conditions encountered when handling incoming requests
        /// will be passed through as dynamic. This allows for a dynamic handling of
        /// members, both for condition matching and for entities returned from the 
        /// selector.
        /// </summary>
        public bool AllowDynamicConditions { get; set; }

        /// <summary>
        /// View descriptions are visible in the AvailableResource resource
        /// </summary>
        public string Description { get; set; }
    }
}