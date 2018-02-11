using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource declaration
    /// </summary>
    public class InvalidResourceDeclaration : RESTarError
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}