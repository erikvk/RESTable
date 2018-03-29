using System;
using System.Net;
using RESTar.Requests;

namespace RESTar.Results.Error {
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