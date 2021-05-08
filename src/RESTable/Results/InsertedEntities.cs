using System.Collections.Generic;
using System.Net;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful insertion of entities
    /// </summary>
    public class InsertedEntities<T> : Change where T : class
    {
        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public IAsyncEnumerable<T> Entities { get; }

        public InsertedEntities(IRequest request, IAsyncEnumerable<T> insertedEntities) : base(request)
        {
            Entities = insertedEntities;
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers.Info = $"{count} entities inserted into '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(InsertedEntities<T>)};{Request.Resource};{Entities}";
    }
}