using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable
{
    /// <inheritdoc cref="IAsyncSelector{T}" />
    /// <inheritdoc cref="IAsyncInserter{T}" />
    /// <inheritdoc cref="IAsyncUpdater{T}" />
    /// <inheritdoc cref="IAsyncDeleter{T}" />
    /// <summary>
    /// The Blank resource is a test and debug resource that does nothing at all.
    /// </summary>
    [RESTable(Description = description)]
    public class Blank : IAsyncSelector<Blank>, IAsyncInserter<Blank>, IAsyncUpdater<Blank>, IAsyncDeleter<Blank>
    {
        private const string description = "A test and debug entity resource that is just an empty set of entities with no properties";

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