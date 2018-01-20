using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal class UpdatedEntities : OK
    {
        internal UpdatedEntities(int count, IRequest request) : base(request)
        {
            Headers["RESTar-info"] = $"{count} entities updated in '{request.Resource.Name}'";
        }
    }

    internal struct WebSocketResult : IFinalizedResult
    {
        internal bool LeaveOpen { get; }
        HttpStatusCode IFinalizedResult.StatusCode => default;
        string IFinalizedResult.StatusDescription => default;
        Stream IFinalizedResult.Body => default;
        string IFinalizedResult.ContentType => default;
        ICollection<string> IFinalizedResult.Cookies => default;
        Headers IFinalizedResult.Headers => default;
        public WebSocketResult(bool leaveOpen) : this() => LeaveOpen = leaveOpen;
    }
}