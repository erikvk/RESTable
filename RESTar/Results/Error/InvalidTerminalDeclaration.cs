using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidTerminalDeclaration : RESTarError
    {
        public InvalidTerminalDeclaration(string message) : base(ErrorCodes.InvalidTerminalDeclaration, message) { }
    }
}
