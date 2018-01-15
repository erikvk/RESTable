using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidExternalResourceProvider : RESTarError
    {
        internal InvalidExternalResourceProvider(string message) : base(ErrorCodes.ResourceProviderError,
            "An error was found in an external ResourceProvider: " + message) { }
    }
}