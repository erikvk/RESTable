using System.Collections.Generic;
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
        public InsertedEntities(IRequest request, int count, object[] entities) : base(request, count, entities)
        {
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers.Info = $"{count} entities inserted into '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(InsertedEntities)};{Request.Resource};{Count}";
    }
}