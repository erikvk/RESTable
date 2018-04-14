using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource wrapper declaration
    /// </summary>
    public class InvalidResourceWrapper : RESTarException
    {
        internal InvalidResourceWrapper(string info) : base(ErrorCodes.ResourceWrapperError, info) { }
    }
}