using System.Net;
using RESTar.Operations;

namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful insertion of entities
    /// </summary>
    public class InsertedEntities : Result
    {
        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        internal InsertedEntities(int count, IRequest request) : base(request)
        {
            InsertedCount = count;
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers["RESTar-info"] = $"{count} entities inserted into '{request.Resource.Name}'";
        }
    }
}