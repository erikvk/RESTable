using RESTar.Internal;

namespace RESTar.Results
{
    public class RemoteBadRequest : BadRequest
    {
        public RemoteBadRequest(ErrorCodes code) : base(code, null) { }
    }
}