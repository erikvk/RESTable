using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class InvalidResourceWrapper : RESTarError
    {
        internal InvalidResourceWrapper(string message) : base(ErrorCodes.ResourceWrapperError, message) { }
    }
}