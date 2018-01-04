using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class FailedResourceAuthentication : Forbidden
    {
        public FailedResourceAuthentication(string message) : base(ErrorCodes.FailedResourceAuthentication, message) { }
    }
}