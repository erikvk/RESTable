using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class InvalidVirtualResourceDeclaration : RESTarError
    {
        internal InvalidVirtualResourceDeclaration(string message) : base(ErrorCodes.InvalidVirtualResourceDeclaration, message) { }
    }
}