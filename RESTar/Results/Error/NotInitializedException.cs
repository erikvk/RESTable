using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a call is made to RESTar before RESTarConfig.Init() has been called.
    /// </summary>
    public class NotInitializedException : RESTarException
    {
        internal NotInitializedException() : base(ErrorCodes.NotInitialized,
            "A RESTar request was created before RESTarConfig.Init() was called. Always " +
            "initialize the RESTar instance before making calls to it.") { }
    }
}