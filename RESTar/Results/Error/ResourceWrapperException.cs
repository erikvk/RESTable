using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when a RESTar resource wrapper was invalid
    /// </summary>
    public class ResourceWrapperException : RESTarException
    {
        internal ResourceWrapperException(string message) : base(ErrorCodes.ResourceWrapperError, message) { }
    }
}