using System.Net;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown from the WebSocket shell when the empty query was used in a request
    /// </summary>
    public class NoQuery : Result
    {
        internal NoQuery(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
        }
    }
}