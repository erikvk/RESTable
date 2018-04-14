using System.Net;
using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown from the WebSocket shell when the shell is in an invalid state to handle incoming
    /// binary data.
    /// </summary>
    public class InvalidShellStateForBinaryInput : Error
    {
        /// <inheritdoc />
        public override string Metadata => $"{nameof(InvalidShellStateForBinaryInput)};{RequestInternal.Resource};{ErrorCode}";

        internal InvalidShellStateForBinaryInput() : base(ErrorCodes.ShellError, "")
        {
            StatusCode = HttpStatusCode.ServiceUnavailable;
            StatusDescription = "Invalid shell state for binary input";
        }
    }
}