using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class NotInitialized : RESTarError
    {
        internal NotInitialized() : base(ErrorCodes.NotInitialized,
            "A RESTar request was created before RESTarConfig.Init() was called. Always " +
            "initialize the RESTar instance before making calls to it.") { }
    }
}