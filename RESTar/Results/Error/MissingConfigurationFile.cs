using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <summary>
    /// Thrown when a RESTar needed a configuration file, but did not get a configuration file path in the call to RESTarConfig.Init
    /// </summary>
    public class MissingConfigurationFile : RESTarException
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}