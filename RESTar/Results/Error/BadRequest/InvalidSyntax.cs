using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a syntax error was discovered when parsing a request
    /// </summary>
    internal class InvalidSyntax : BadRequest
    {
        internal InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) { }
    }
}