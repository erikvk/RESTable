using System.IO;
using System.Net;
using RESTar.Internal;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal class UpdatedEntities : OK
    {
        internal UpdatedEntities(int count, IEntityResource resource)
        {
            Headers["RESTar-info"] = $"{count} entities updated in '{resource.FullName}'";
        }
    }

    internal struct WebSocketResult : IFinalizedResult
    {
        internal bool LeaveOpen { get; }
        HttpStatusCode IFinalizedResult.StatusCode => default;
        string IFinalizedResult.StatusDescription => default;
        Stream IFinalizedResult.Body => default;
        string IFinalizedResult.ContentType => default;
        Headers IFinalizedResult.Headers => default;
        public WebSocketResult(bool leaveOpen) : this() => LeaveOpen = leaveOpen;
    }
}