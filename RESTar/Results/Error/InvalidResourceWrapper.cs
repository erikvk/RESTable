using RESTar.Internal;

namespace RESTar.Results.Fail
{
    internal class InvalidResourceWrapper : RESTarError
    {
        internal InvalidResourceWrapper(string message) : base(ErrorCodes.ResourceWrapperError, message) { }
    }
}