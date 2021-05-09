using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful update of entities
    /// </summary>
    public class UpdatedEntities<T> : Change<T> where T :class
    {
        public UpdatedEntities(IRequest request, int count, T[] entities) : base(request, count, entities)
        {
            Headers.Info = $"{count} entities updated in '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(UpdatedEntities<T>)};{Request.Resource};{Count}";
    }
}