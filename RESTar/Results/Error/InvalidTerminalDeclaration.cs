using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid terminal resource declaration
    /// </summary>
    public class InvalidTerminalDeclaration : RESTarError
    {
        internal InvalidTerminalDeclaration(string message) : base(ErrorCodes.InvalidTerminalDeclaration, message) { }
    }
}
