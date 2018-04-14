using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid enum declaration was referenced from a RESTar resource
    /// </summary>
    public class InvalidReferencedEnumDeclaration : RESTarException
    {
        internal InvalidReferencedEnumDeclaration(string info) : base(ErrorCodes.InvalidReferencedEnumDeclaration, info) { }
    }
}