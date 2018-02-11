using RESTar.Admin;

namespace RESTar
{
    /// <summary>
    /// DatabaseIndexers provide interfaces for managing database indexes for some 
    /// group of resources.
    /// </summary>
    public interface IDatabaseIndexer : ISelector<DatabaseIndex>, IInserter<DatabaseIndex>, IUpdater<DatabaseIndex>,
        IDeleter<DatabaseIndex> { }
}