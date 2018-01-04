using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class MissingConfigurationFile : RESTarError
    {
        internal MissingConfigurationFile(string message) : base(ErrorCodes.MissingConfigurationFile, message) { }
    }
}