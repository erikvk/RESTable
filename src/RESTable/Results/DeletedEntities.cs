﻿using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful deletion of entities
    /// </summary>
    public class DeletedEntities<T> : Change<T> where T : class
    {
        /// <summary>
        /// The number of entities deleted
        /// </summary>
        public int DeletedCount { get; }

        public DeletedEntities(IRequest request, int count) : base(request, count, new T[0])
        {
            DeletedCount = count;
            Headers.Info = $"{count} entities deleted from '{request.Resource}'";
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(DeletedEntities<T>)};{Request.Resource};{DeletedCount}";
    }
}