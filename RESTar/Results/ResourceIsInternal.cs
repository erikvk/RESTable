using RESTar.Internal;
using RESTar.Meta;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an external request for an internal resource
    /// search string.
    /// </summary>
    public class ResourceIsInternal : Forbidden
    {
        internal ResourceIsInternal(IResource resource) : base(ErrorCodes.ResourceIsInternal,
            $"Cannot make an external request to internal resource '{resource.Name}'") { }
    }
}