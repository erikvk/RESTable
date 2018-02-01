using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidVirtualResourceDeclaration : RESTarError
    {
        internal InvalidVirtualResourceDeclaration(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}