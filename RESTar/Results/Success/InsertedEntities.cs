using System.Net;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    internal class InsertedEntities : Result
    {
        internal InsertedEntities(int count, IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.Created;
            StatusDescription = "Created";
            Headers["RESTar-info"] = $"{count} entities inserted into '{request.Resource.Name}'";
        }
    }
}