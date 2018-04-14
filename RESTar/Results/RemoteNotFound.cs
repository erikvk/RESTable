using RESTar.Internal;

namespace RESTar.Results
{
    public class RemoteNotFound : NotFound
    {
        public RemoteNotFound(ErrorCodes code) : base(code, null) { }
    }
}