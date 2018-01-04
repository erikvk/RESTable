using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class MethodUnavailable : Base
    {
        public MethodUnavailable(Methods method, IResource resource) : base(ErrorCodes.NotAuthorized,
            $"{method} is not available for resource '{resource.Name}'") { }
    }
}