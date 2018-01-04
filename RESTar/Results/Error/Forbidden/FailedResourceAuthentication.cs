using RESTar.Internal;

namespace RESTar.Results.Fail.Forbidden
{
    internal class FailedResourceAuthentication : Forbidden
    {
        public FailedResourceAuthentication(string message) : base(ErrorCodes.FailedResourceAuthentication, message) { }
    }
}