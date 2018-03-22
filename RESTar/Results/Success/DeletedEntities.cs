namespace RESTar.Results.Success
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

        internal DeletedEntities(int count, IRequest request) : base(request)
        {
            DeletedCount = count;
            Headers["RESTar-info"] = $"{count} entities deleted from '{request.Resource.Name}'";
        }
    }
}