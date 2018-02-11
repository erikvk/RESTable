using RESTar.Internal;

namespace RESTar.Results.Error
{
    public class InvalidResourceWrapper : RESTarError
    {
        internal InvalidResourceWrapper(string message) : base(ErrorCodes.ResourceWrapperError, message) { }
    }
}