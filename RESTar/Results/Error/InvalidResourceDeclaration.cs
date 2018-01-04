using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidResourceDeclaration : RESTarException
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}