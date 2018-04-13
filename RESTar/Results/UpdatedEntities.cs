namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful update of entities
    /// </summary>
    public class UpdatedEntities : OK
    {
        /// <summary>
        /// The number of entities updated
        /// </summary>
        public int UpdatedCount { get; }

        /// <inheritdoc />
        public override string Metadata => $"{GetType()};{UpdatedCount};";

        internal UpdatedEntities(int count, IRequest request) : base(request)
        {
            UpdatedCount = count;
            Headers.Info = $"{count} entities updated in '{request.Resource.Name}'";
        }
    }
}