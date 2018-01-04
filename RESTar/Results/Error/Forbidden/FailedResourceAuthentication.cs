using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class FailedResourceAuthentication : Base
    {
        public FailedResourceAuthentication(string message) : base(ErrorCodes.FailedResourceAuthentication, message) { }
    }
}