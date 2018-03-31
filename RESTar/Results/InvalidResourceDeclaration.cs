using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource declaration
    /// </summary>
    public class InvalidResourceDeclaration : Error
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}