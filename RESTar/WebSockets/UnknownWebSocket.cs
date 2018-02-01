using RESTar.Internal;
using RESTar.Results.Error;

namespace RESTar.WebSockets
{
    internal class UnknownWebSocketId : RESTarError
    {
        public UnknownWebSocketId(string message) : base(ErrorCodes.UnknownWebSocketId, message) { }
    }
}