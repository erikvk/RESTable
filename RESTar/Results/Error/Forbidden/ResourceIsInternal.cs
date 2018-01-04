using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a client tries to make an external request to an internal resource
    /// </summary>
    public class ResourceIsInternal : Base
    {
        internal ResourceIsInternal(IResource resource) : base(ErrorCodes.ResourceIsInternal,
            $"Cannot make an external request to internal resource '{resource.Name}'") { }
    }
}