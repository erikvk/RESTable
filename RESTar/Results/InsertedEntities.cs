using System.Net;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful insertion of entities
    /// </summary>
    public class InsertedEntities : OK
    {
        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        /// <inheritdoc />
        public override string Metadata => $"{GetType()};{InsertedCount};";

        internal InsertedEntities(int count, IRequest trace) : base(trace)
        {
            InsertedCount = count;
            StatusCode = count < 1 ? HttpStatusCode.OK : HttpStatusCode.Created;
            StatusDescription = StatusCode.ToString();
            Headers.Info = $"{count} entities inserted into '{trace.Resource.Name}'";
        }
    }
}