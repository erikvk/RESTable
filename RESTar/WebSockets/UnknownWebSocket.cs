using RESTar.Internal;
using RESTar.Results;

namespace RESTar.WebSockets
{
    internal class UnknownWebSocketId : Error
    {
        public UnknownWebSocketId(string info) : base(ErrorCodes.UnknownWebSocketId, info) { }
    }
}