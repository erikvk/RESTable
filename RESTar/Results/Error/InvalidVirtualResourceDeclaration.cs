using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidVirtualResourceDeclaration : RESTarException
    {
        internal InvalidVirtualResourceDeclaration(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}