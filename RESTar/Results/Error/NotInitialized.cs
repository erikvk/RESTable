using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar receives requests or other operations before it's properly initialized
    /// </summary>
    public class NotInitialized : RESTarError
    {
        internal NotInitialized() : base(ErrorCodes.NotInitialized,
            "A RESTar request was created before RESTarConfig.Init() was called. Always " +
            "initialize the RESTar instance before making calls to it.") { }
    }
}