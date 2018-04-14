using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external protocol provider
    /// </summary>
    public class InvalidProtocolProvider : RESTarException
    {
        internal InvalidProtocolProvider(string message) : base(ErrorCodes.InvalidProtocolProvider,
            "An error was found in an external IProtocolProvider: " + message) { }
    }
}