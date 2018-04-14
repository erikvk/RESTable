using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external protocol provider
    /// </summary>
    public class InvalidContentTypeProvider : RESTarException
    {
        internal InvalidContentTypeProvider(string message) : base(ErrorCodes.InvalidContentTypeProvider,
            "An error was found in an external IContentTypeProvider: " + message) { }
    }
}