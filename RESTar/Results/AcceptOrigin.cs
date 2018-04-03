using System.Net;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on a successful CORS preflight
    /// </summary>
    public class AcceptOrigin : Success
    {
        internal AcceptOrigin(string origin, RequestParameters parameters) : base(parameters)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "OK";
            Headers = new Headers
            {
                ["Access-Control-Allow-Origin"] = RESTarConfig.AllowAllOrigins ? "*" : origin,
                ["Access-Control-Allow-Methods"] = string.Join(", ", parameters.IResource.AvailableMethods),
                ["Access-Control-Max-Age"] = "120",
                ["Access-Control-Allow-Credentials"] = "true",
                ["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, source, destination"
            };
        }
    }
}