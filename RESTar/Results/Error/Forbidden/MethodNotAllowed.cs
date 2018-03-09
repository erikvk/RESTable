using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error.Forbidden
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters a request with an unallowed method (according to access rights or resource declaration)
    /// search string.
    /// </summary>
    public class MethodNotAllowed : RESTarError
    {
        /// <inheritdoc />
        public MethodNotAllowed(Methods method, ITarget target, bool failedAuth) : base(ErrorCodes.MethodNotAllowed,
            $"Method '{method}' is not available for resource '{target.Name}'{(failedAuth ? " for the current API key" : "")}")
        {
            StatusCode = HttpStatusCode.MethodNotAllowed;
            StatusDescription = "Method not allowed";
        }
    }
}