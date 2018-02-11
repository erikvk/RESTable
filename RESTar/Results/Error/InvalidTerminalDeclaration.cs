using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidTerminalDeclaration : RESTarError
    {
        internal InvalidTerminalDeclaration(string message) : base(ErrorCodes.InvalidTerminalDeclaration, message) { }
    }
}
