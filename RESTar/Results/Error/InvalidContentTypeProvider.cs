using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an error with an external protocol provider
    /// </summary>
    public class InvalidContentTypeProvider : RESTarError
    {
        internal InvalidContentTypeProvider(string message) : base(ErrorCodes.InvalidContentTypeProvider,
            "An error was found in an external IContentTypeProvider: " + message) { }
    }
}