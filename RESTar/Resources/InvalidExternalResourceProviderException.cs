namespace RESTar.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external resource provider
    /// </summary>
    public class InvalidExternalResourceProviderException : RESTarException
    {
        internal InvalidExternalResourceProviderException(string message) : base(ErrorCodes.EntityResourceProviderError,
            "An error was found in an external ResourceProvider: " + message) { }
    }
}