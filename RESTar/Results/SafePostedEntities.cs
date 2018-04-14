namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful safe post insertion/updating
    /// </summary>
    public class SafePostedEntities : OK
    {
        /// <summary>
        /// The number of updated entities
        /// </summary>
        public int UpdatedCount { get; }

        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        /// <inheritdoc />
        public override string Metadata => $"{GetType()};{Request.Resource};{UpdatedCount},{InsertedCount}";

        internal SafePostedEntities(int updatedEntities, int insertedEntities, IRequest request) : base(request)
        {
            UpdatedCount = updatedEntities;
            InsertedCount = insertedEntities;
            Headers.Info = $"Updated {updatedEntities} and then inserted {insertedEntities} entities in resource '{request.Resource}'";
        }
    }
}