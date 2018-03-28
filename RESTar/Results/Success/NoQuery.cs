using System;
using System.Net;
using RESTar.Requests;

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

    /// <inheritdoc />
    /// <summary>
    /// Thrown from the WebSocket shell when the shell is in an invalid state to handle incoming
    /// binary data.
    /// </summary>
    public class InvalidShellStateForBinaryInput : Result
    {
        /// <inheritdoc />
        public override TimeSpan TimeElapsed { get; protected set; }

        internal InvalidShellStateForBinaryInput(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.ServiceUnavailable;
            StatusDescription = "Invalid shell state for binary input";
            TimeElapsed = default;
        }
    }
}