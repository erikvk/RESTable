using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    public class FailedResourceAuthentication : Forbidden
    {
        public FailedResourceAuthentication(string message) : base(ErrorCodes.FailedResourceAuthentication, message) { }
    }
}