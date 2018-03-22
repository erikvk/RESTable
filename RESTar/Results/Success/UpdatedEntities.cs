namespace RESTar.Results.Success
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful update of entities
    /// </summary>
    public class UpdatedEntities : OK
    {
        /// <summary>
        /// The number of entitites updated
        /// </summary>
        public int UpdatedCount { get; }

        internal UpdatedEntities(int count, IRequest request) : base(request)
        {
            UpdatedCount = count;
            Headers["RESTar-info"] = $"{count} entities updated in '{request.Resource.Name}'";
        }
    }
}