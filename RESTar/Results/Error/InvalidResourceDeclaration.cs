using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidResourceDeclaration : RESTarError
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }

    internal class InvalidReferencedEnumDeclaration : RESTarError
    {
        internal InvalidReferencedEnumDeclaration(string message) : base(ErrorCodes.InvalidReferencedEnumDeclaration, message) { }
    }
}