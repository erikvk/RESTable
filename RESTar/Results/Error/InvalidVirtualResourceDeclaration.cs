using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidVirtualResourceDeclaration : RESTarError
    {
        internal InvalidVirtualResourceDeclaration(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}