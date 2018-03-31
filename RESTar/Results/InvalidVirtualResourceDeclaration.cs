using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid virtual resource declaration
    /// </summary>
    public class InvalidVirtualResourceDeclaration : Error
    {
        internal InvalidVirtualResourceDeclaration(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}