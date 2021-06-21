using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Results
{
    public abstract class Change : OK
    {
        public const int MaxNumberOfEntitiesInChangeResults = 25;

        /// <summary>
        /// The number of changed entities
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// True if the number of entities changed exceeded the maximum number of entities
        /// that can be included in the Entities array of this result.
        /// </summary>
        public bool TooManyEntities => Count > MaxNumberOfEntitiesInChangeResults;

        /// <summary>
        /// Gets the changed non-deleted entities in case of TooManyEntities being false. Else this enumeratoin is empty.
        /// </summary>
        public abstract IEnumerable<object> GetEntities();

        protected Change(IRequest request, int count) : base(request)
        {
            Count = count;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// A result that encodes a change in a resource, for example an update or insert
    /// </summary>
    public abstract class Change<T> : Change where T : class
    {
        /// <summary>
        /// The changed non-deleted entities in case of TooManyEntities being false. Else this array is empty.
        /// </summary>
        public IReadOnlyCollection<T> Entities { get; }

        public override IEnumerable<object> GetEntities() => Entities;

        /// <inheritdoc />
        protected Change(IRequest request, int count, IReadOnlyCollection<T> entities) : base(request, count)
        {
            Entities = entities;
        }
    }
}