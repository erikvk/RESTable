using RESTar.Internal;

namespace RESTar.Results.Fail.BadRequest
{
    internal class InvalidSyntax : BadRequest
    {
        internal InvalidSyntax(ErrorCodes errorCode, string message) : base(errorCode,
            "Syntax error: " + message) { }
    }
}