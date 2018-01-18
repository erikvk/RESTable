using System.Net;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    internal class InsertedEntities : Result
    {
        internal InsertedEntities(int count, IEntityResource resource)
        {
            StatusCode = HttpStatusCode.Created;
            StatusDescription = "Created";
            Headers["RESTar-info"] = $"{count} entities inserted into '{resource.FullName}'";
        }
    }
}