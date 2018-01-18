using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    internal class MethodNotAllowed : RESTarError
    {
        public MethodNotAllowed(Methods method, ITarget target) : base(ErrorCodes.MethodNotAllowed,
            $"Method '{method}' is not available for resource '{target.FullName}'")
        {
            StatusCode = HttpStatusCode.MethodNotAllowed;
            StatusDescription = "Method not allowed";
        }
    }
}