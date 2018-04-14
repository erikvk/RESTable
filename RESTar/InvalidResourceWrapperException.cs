using RESTar.Internal;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource wrapper declaration
    /// </summary>
    public class InvalidResourceWrapperException : RESTarException
    {
        internal InvalidResourceWrapperException(string info) : base(ErrorCodes.ResourceWrapperError, info) { }
    }
}