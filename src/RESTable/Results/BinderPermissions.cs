using System;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an error with permissions for binding 
    /// against, for example, properties of resources.
    /// </summary>
    internal class BinderPermissions : Internal
    {
        /// <inheritdoc />
        public BinderPermissions(Exception e) : base(ErrorCodes.FailedBinding, 
            "RESTable failed to resolve a reference to a property or type.", e) { }
    }
}