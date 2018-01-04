using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    internal class InvalidSyntax : BadRequest
    {
        internal InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error while parsing request: " + message) { }
    }
}