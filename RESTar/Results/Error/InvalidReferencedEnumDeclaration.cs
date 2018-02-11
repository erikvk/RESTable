using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid enum declaration was referenced from a RESTar resource
    /// </summary>
    public class InvalidReferencedEnumDeclaration : RESTarError
    {
        internal InvalidReferencedEnumDeclaration(string message) : base(ErrorCodes.InvalidReferencedEnumDeclaration, message) { }
    }
}