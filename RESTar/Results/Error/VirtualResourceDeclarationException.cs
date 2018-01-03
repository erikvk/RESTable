using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an error was detected in a virtual resource declaration.
    /// </summary>
    public class VirtualResourceDeclarationException : RESTarException
    {
        internal VirtualResourceDeclarationException(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}