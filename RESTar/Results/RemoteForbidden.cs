using RESTar.Internal;

namespace RESTar.Results
{
    public class RemoteForbidden : Forbidden
    {
        public RemoteForbidden(ErrorCodes code) : base(code, null) { }
    }
}