using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    /// <inheritdoc />
    public class InvalidSyntax : BadRequest
    {
        /// <inheritdoc />
        public InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error: " + message) { }
    }
}