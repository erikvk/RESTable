﻿using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful safe post insertion/updating
    /// </summary>
    public class SafePostedEntities : Change
    {
        /// <summary>
        /// The number of updated entities
        /// </summary>
        public int UpdatedCount { get; }

        /// <summary>
        /// The number of inserted entities
        /// </summary>
        public int InsertedCount { get; }

        public SafePostedEntities(IRequest request, int updatedEntities, int insertedEntities, object[] entities) : base(request, updatedEntities + insertedEntities, entities)
        {
            UpdatedCount = updatedEntities;
            InsertedCount = insertedEntities;
            Headers.Info = $"Updated {updatedEntities} and then inserted {insertedEntities} entities in resource '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(SafePostedEntities)};{Request.Resource};{UpdatedCount},{InsertedCount}";
    }
}