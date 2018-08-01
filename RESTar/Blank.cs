using System.Collections.Generic;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;

namespace RESTar
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// The Blank resource is a test and debug resource that does nothing at all.
    /// </summary>
    [RESTar]
    public class Blank : ISelector<Blank>, IInserter<Blank>, IUpdater<Blank>, IDeleter<Blank>
    {
        private const string description = "A test and debug resource that does nothing at all.";

        /// <inheritdoc />
        public IEnumerable<Blank> Select(IRequest<Blank> request) => null;

        /// <inheritdoc />
        public int Insert(IRequest<Blank> request) => 0;

        /// <inheritdoc />
        public int Update(IRequest<Blank> request) => 0;

        /// <inheritdoc />
        public int Delete(IRequest<Blank> request) => 0;
    }
}