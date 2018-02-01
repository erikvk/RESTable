using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class MissingConfigurationFile : RESTarError
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}