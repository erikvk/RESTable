using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    internal class ResourceDeclarationException : RESTarException
    {
        internal ResourceDeclarationException(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}