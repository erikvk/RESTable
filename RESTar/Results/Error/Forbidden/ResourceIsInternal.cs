using RESTar.Internal;

namespace RESTar.Results.Fail.Forbidden
{
    internal class ResourceIsInternal : Forbidden
    {
        internal ResourceIsInternal(IResource resource) : base(ErrorCodes.ResourceIsInternal,
            $"Cannot make an external request to internal resource '{resource.Name}'") { }
    }
}