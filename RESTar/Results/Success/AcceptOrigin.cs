using RESTar.Queries;
using static RESTar.RESTarConfig;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on a successful CORS preflight
    /// </summary>
    public class AcceptOrigin : OK
    {
        internal AcceptOrigin(string origin, QueryParameters parameters) : base(parameters)
        {
            Headers["Access-Control-Allow-Origin"] = AllowAllOrigins ? "*" : origin;
            Headers["Access-Control-Allow-Methods"] = string.Join(", ", parameters.IResource.AvailableMethods);
            Headers["Access-Control-Max-Age"] = "120";
            Headers["Access-Control-Allow-Credentials"] = "true";
            Headers["Access-Control-Allow-Headers"] = "origin, content-type, accept, authorization, source, destination";
            TimeElapsed = parameters.Stopwatch.Elapsed;
        }
    }
}