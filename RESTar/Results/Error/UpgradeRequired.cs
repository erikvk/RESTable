using System.Net;
using RESTar.Internal;

namespace RESTar.Results.Error
{
    internal class UpgradeRequired : RESTarError
    {
        internal UpgradeRequired(string terminalName) : base(ErrorCodes.UpgradeRequired,
            $"Connections to terminal resource {terminalName} must include a WebSocket upgrade request")
        {
            StatusCode = HttpStatusCode.UpgradeRequired;
            StatusDescription = "Upgrade required";
        }
    }
}