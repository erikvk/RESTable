using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar requires a configuration file for setting up API keys and/or CORS origins, but
    /// no configuration file was found.
    /// </summary>
    public class MissingConfigurationFile : RESTarError
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}