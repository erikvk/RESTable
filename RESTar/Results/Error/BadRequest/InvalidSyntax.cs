using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    public class InvalidSyntax : BadRequest
    {
        /// <inheritdoc />
        public InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode, "Syntax error: " + message) { }
    }
}