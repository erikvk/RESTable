using System.Net;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    internal class InsertedEntities : Result
    {
        internal InsertedEntities(int count, IRequest request) : base(request)
        {
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers["RESTar-info"] = $"{count} entities inserted into '{request.Resource.Name}'";
        }
    }
}