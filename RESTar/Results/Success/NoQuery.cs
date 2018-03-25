using System;
using System.Net;
using RESTar.Operations;
using RESTar.Queries;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown from the WebSocket shell when the empty query was used in a request
    /// </summary>
    public class NoQuery : Result
    {
        /// <inheritdoc />
        public override TimeSpan TimeElapsed { get; protected set; }

        internal NoQuery(ITraceable trace, TimeSpan elapsed) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = elapsed;
        }
    }
}