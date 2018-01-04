using RESTar.Internal;

namespace RESTar.Results.Fail.Forbidden
{
    internal class MethodUnavailable : Forbidden
    {
        public MethodUnavailable(Methods method, IResource resource) : base(ErrorCodes.NotAuthorized,
            $"{method} is not available for resource '{resource.Name}'") { }
    }
}