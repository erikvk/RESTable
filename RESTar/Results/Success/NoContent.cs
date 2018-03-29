using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client when no content was selected in a request
    /// </summary>
    public class NoContent : Success
    {
        internal NoContent(ITraceable trace, TimeSpan elapsed) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            Headers["RESTar-info"] = "No entities found matching request.";
            TimeElapsed = elapsed;
        }
    }
}