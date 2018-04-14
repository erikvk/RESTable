using RESTar.Internal;

namespace RESTar.Results
{
    public class RemoteInternal : Internal
    {
        public RemoteInternal(ErrorCodes code) : base(code, null) { }
    }
}