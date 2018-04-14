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

        internal UpdatedEntities(int count, IRequest request) : base(request)
        {
            UpdatedCount = count;
            Headers.Info = $"{count} entities updated in '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(UpdatedEntities)};{Request.Resource};{UpdatedCount}";
    }
}