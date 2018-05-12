using System;
using System.Collections.Generic;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Registers a class as an internal RESTar resource, that can only be used in internal 
    /// requests (using the RESTar.Request`1 class). If no methods are provided in the 
    /// constructor, all methods are made available for this resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class RESTarInternalAttribute : RESTarAttribute
    {
        /// <inheritdoc />
        internal RESTarInternalAttribute(IReadOnlyList<Method> methods) : base(methods) { }

        /// <inheritdoc />
        public RESTarInternalAttribute(params Method[] methodRestrictions) : base(methodRestrictions) { }
    }
}