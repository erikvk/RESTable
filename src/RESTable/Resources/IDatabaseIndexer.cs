using RESTable.Admin;
using RESTable.Resources.Operations;

namespace RESTable.Resources
{
    /// <inheritdoc cref="ISelector{T}" />
    /// <inheritdoc cref="IInserter{T}" />
    /// <inheritdoc cref="IUpdater{T}" />
    /// <inheritdoc cref="IDeleter{T}" />
    /// <summary>
    /// DatabaseIndexers provide interfaces for managing database indexes for some 
    /// group of resources.
    /// </summary>
    public interface IDatabaseIndexer : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex> { }
}