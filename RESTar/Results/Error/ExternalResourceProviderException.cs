using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar resource provider was found invalid
    /// </summary>
    public class ExternalResourceProviderException : RESTarException
    {
        internal ExternalResourceProviderException(string message) : base(ErrorCodes.ResourceProviderError,
            "An error was found in an external ResourceProvider: " + message) { }
    }
}