using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external protocol provider
    /// </summary>
    public class InvalidProtocolProvider : RESTarError
    {
        internal InvalidProtocolProvider(string message) : base(ErrorCodes.InvalidProtocolProvider,
            "An error was found in an external IProtocolProvider: " + message) { }
    }
}