using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown from the WebSocket shell when the empty query was used in a request
    /// </summary>
    public class NoQuery : Success
    {
        internal NoQuery(ITraceable trace, TimeSpan elapsed) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = elapsed;
        }
    }
}