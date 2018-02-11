using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class MissingConfigurationFile : RESTarError
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}