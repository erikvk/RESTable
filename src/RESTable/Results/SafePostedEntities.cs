using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful safe post insertion/updating
    /// </summary>
    public class SafePostedEntities<T> : Change<T> where T : class
    {
        /// <summary>
        /// The number of updated entities
        /// </summary>
        public int UpdatedCount { get; }

        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        public SafePostedEntities(IRequest request, int updatedCount, int insertedCount, IReadOnlyCollection<T> entities) : base(request, updatedCount + insertedCount, entities)
        {
            UpdatedCount = updatedCount;
            InsertedCount = insertedCount;
            Headers.Info = $"Updated {updatedCount} and then inserted {insertedCount} entities in resource '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(SafePostedEntities<T>)};{Request.Resource};{UpdatedCount},{InsertedCount}";
    }
}