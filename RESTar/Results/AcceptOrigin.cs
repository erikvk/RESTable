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
            Headers["Access-Control-Allow-Origin"] = RESTarConfig.AllowAllOrigins ? "*" : origin;
            Headers["Access-Control-Allow-Methods"] = string.Join(", ", parameters.IResource.AvailableMethods);
            Headers["Access-Control-Max-Age"] = "120";
            Headers["Access-Control-Allow-Credentials"] = "true";
            Headers["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, source, destination";
        }
    }
}