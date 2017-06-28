using System.Collections.Generic;

namespace RESTar
{
    /// <summary>
    /// Interface used to register a Selector for a given resource type
    /// </summary>
    public interface ISelector<out T>
    {
        /// <summary>
        /// The select method for this ISelector instance. Defines the Select
        /// operation for a given resource.
        /// </summary>
        IEnumerable<T> Select(IRequest request);
    }

    /// <summary>
    /// Interface used to register an Inserter for a given resource type
    /// </summary>
    public interface IInserter<in T>
    {
        /// <summary>
        /// The insert method for this IInserter instance. Defines the Insert
        /// operation for a given resource.
        /// </summary>
        int Insert(IEnumerable<T> entities, IRequest request);
    }

    /// <summary>
    /// Interface used to register an Updater for a given resource type
    /// </summary>
    public interface IUpdater<in T>
    {
        /// <summary>
        /// The update method for this IUpdater instance. Defines the Update
        /// operation for a given resource.
        /// </summary>
        int Update(IEnumerable<T> entities, IRequest request);
    }

    /// <summary>
    /// Interface used to register a Deleter for a given resource type
    /// </summary>
    public interface IDeleter<in T>
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        int Delete(IEnumerable<T> entities, IRequest request);
    }

    internal interface IOperationsProvider<T> :
        ISelector<T>,
        IInserter<T>,
        IUpdater<T>,
        IDeleter<T>
    {
    }
}