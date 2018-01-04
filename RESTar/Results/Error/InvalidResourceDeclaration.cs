using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class InvalidResourceDeclaration : RESTarError
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}