using RESTar.Internal;

namespace RESTar.Results.Error
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar encounters an invalid resource wrapper declaration
    /// </summary>
    public class InvalidResourceWrapper : RESTarError
    {
        internal InvalidResourceWrapper(string message) : base(ErrorCodes.ResourceWrapperError, message) { }
    }
}