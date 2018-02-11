using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid virtual resource declaration
    /// </summary>
    public class InvalidVirtualResourceDeclaration : RESTarError
    {
        internal InvalidVirtualResourceDeclaration(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}