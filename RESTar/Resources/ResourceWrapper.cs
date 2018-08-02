// ReSharper disable UnusedTypeParameter

using RESTar.Meta.Internal;

namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// ResourceWrapper instances enable resource declarations for resources in read-only 
    /// assemblies and/or in assemblies where a reference to RESTar is not practical.
    /// </summary>
    public abstract class ResourceWrapper<T> : IResourceWrapper where T : class
    {
        /// <summary>
        /// Defines the custom validation logic for this resource wrapper
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <param name="invalidReason">The reason for the entity not being valid</param>
        [MethodNotImplemented]
        protected virtual bool IsValid(T entity, out string invalidReason)
        {
            invalidReason = null;
            return true;
        }
    }
}