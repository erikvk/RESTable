using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    public class MethodUnavailable : Forbidden
    {
        public MethodUnavailable(Methods method, IEntityResource resource) : base(ErrorCodes.NotAuthorized,
            $"{method} is not available for resource '{resource.Name}'") { }
    }
}