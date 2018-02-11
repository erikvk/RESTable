using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidResourceDeclaration : RESTarError
    {
        internal InvalidResourceDeclaration(string message) : base(ErrorCodes.InvalidResourceDeclaration, message) { }
    }
}