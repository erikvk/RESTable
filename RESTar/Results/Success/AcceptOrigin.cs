using RESTar.Internal;
using RESTar.Requests;
using static RESTar.RESTarConfig;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on a successful CORS preflight
    /// </summary>
    public class AcceptOrigin : OK
    {
        internal AcceptOrigin(string origin, IResource resource, ITraceable trace) : base(trace)
        {
            Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : origin;
            Headers["Access-Control-Allow-Methods"] = string.Join(", ", resource.AvailableMethods);
            Headers["Access-Control-Max-Age"] = "120";
            Headers["Access-Control-Allow-Credentials"] = "true";
            Headers["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, source, destination";
        }
    }
}