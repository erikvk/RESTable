using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar add-on could not be connected properly
    /// </summary>
    public class RESTarAddOnException : RESTarException
    {
        internal RESTarAddOnException(string message) : base(ErrorCodes.AddOnError, message) { }
    }
}