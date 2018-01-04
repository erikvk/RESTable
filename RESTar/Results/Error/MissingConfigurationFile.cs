using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class MissingConfigurationFile : RESTarException
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}