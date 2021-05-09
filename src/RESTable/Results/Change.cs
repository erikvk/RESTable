using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// A result that encodes a change in a resource, for example an update or insert
    /// </summary>
    public abstract class Change : OK
    {
        public const int MaxNumberOfEntitiesInChangeResults = 100;

        /// <summary>
        /// The number of changed entities
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// The changed entities, in case that at most 100 entities were changed. If more
        /// than 100 entities were changed, this property is null.
        /// </summary>
        public object[] Entities { get; }

        /// <summary>
        /// True if the number of entities changed exceeded the maximum number of entities
        /// that can be included in the Entities array of this result.
        /// </summary>
        public bool TooManyEntities => Count > MaxNumberOfEntitiesInChangeResults;
        
        /// <inheritdoc />
        protected Change(IRequest request, int count, object[] entities) : base(request)
        {
            Count = count;
            Entities = entities;
        }
    }
}