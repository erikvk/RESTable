using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidProtocolProvider : RESTarError
    {
        internal InvalidProtocolProvider(string message) : base(ErrorCodes.InvalidProtocolProvider,
            "An error was found in an external ProtocolProvider: " + message) { }
    }
}