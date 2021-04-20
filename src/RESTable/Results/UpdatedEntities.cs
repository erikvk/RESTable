using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful update of entities
    /// </summary>
    public class UpdatedEntities : Change
    {
        /// <summary>
        /// The number of entities updated
        /// </summary>
        public int UpdatedCount { get; }

        public UpdatedEntities(IRequest request, int count) : base(request)
        {
            UpdatedCount = count;
            Headers.Info = $"{count} entities updated in '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(UpdatedEntities)};{Request.Resource};{UpdatedCount}";
    }
}