using RESTar.Internal;

namespace RESTar.Results.Error {
    public class InvalidReferencedEnumDeclaration : RESTarError
    {
        internal InvalidReferencedEnumDeclaration(string message) : base(ErrorCodes.InvalidReferencedEnumDeclaration, message) { }
    }
}