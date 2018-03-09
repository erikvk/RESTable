using RESTar.Internal;

namespace RESTar.Results.Error.NotFound
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTar receives a request with an unknown protocol indicator
    /// </summary>
    public class UnknownProtocol : NotFound
    {
        internal UnknownProtocol(string searchString) : base(ErrorCodes.UnknownProtocol, $"Could not identify any protocol by '{searchString}'") { }
    }
}