using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidResourceDeclaration : RESTarError
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}