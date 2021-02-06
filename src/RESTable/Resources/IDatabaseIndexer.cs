using RESTable.Admin;
using RESTable.Resources.Operations;

namespace RESTable.Resources
{
    /// <inheritdoc cref="IAsyncSelector{T}" />
    /// <inheritdoc cref="IAsyncInserter{T}" />
    /// <inheritdoc cref="IAsyncUpdater{T}" />
    /// <inheritdoc cref="IAsyncDeleter{T}" />
    /// <summary>
    /// DatabaseIndexers provide interfaces for managing database indexes for some 
    /// group of resources.
    /// </summary>
    public interface IDatabaseIndexer : IAsyncSelector<DatabaseIndex>, IAsyncInserter<DatabaseIndex>, IAsyncUpdater<DatabaseIndex>,
        IAsyncDeleter<DatabaseIndex> { }
}