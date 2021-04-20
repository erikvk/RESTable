using System.Net;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful insertion of entities
    /// </summary>
    public class InsertedEntities : Change
    {
        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        public InsertedEntities(IRequest request, int count) : base(request)
        {
            InsertedCount = count;
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers.Info = $"{count} entities inserted into '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(InsertedEntities)};{Request.Resource};{InsertedCount}";
    }
}