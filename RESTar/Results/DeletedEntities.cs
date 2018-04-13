namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful deletion of entities
    /// </summary>
    public class DeletedEntities : OK
    {
        /// <summary>
        /// The number of entities deleted
        /// </summary>
        public int DeletedCount { get; }

        /// <inheritdoc />
        public override string Metadata => $"{GetType()};{Request.Resource};{DeletedCount}";

        internal DeletedEntities(int count, IRequest request) : base(request)
        {
            DeletedCount = count;
            Headers.Info = $"{count} entities deleted from '{request.Resource.Name}'";
        }
    }
}