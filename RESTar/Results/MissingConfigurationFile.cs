using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar requires a configuration file for setting up API keys and/or CORS origins, but
    /// no configuration file was found.
    /// </summary>
    public class MissingConfigurationFile : Error
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}