using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidResourceWrapper : RESTarException
    {
        internal InvalidResourceWrapper(string message) : base(ErrorCodes.ResourceWrapperError, message) { }
    }
}