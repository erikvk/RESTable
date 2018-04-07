using RESTar.Internal;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource wrapper declaration
    /// </summary>
    public class InvalidResourceWrapper : Error
    {
        internal InvalidResourceWrapper(string info) : base(ErrorCodes.ResourceWrapperError, info) { }
    }
}