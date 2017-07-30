using System.Collections.Generic;

namespace RESTar
{
    /// <summary>
    /// Interface used to register a Selector for a given resource type
    /// </summary>
    public interface ISelector<T> where T : class
    {
        /// <summary>
        /// The select method for this ISelector instance. Defines the Select
        /// operation for a given resource.
        /// </summary>
        IEnumerable<T> Select(IRequest<T> request);
    }

    /// <summary>
    /// Interface used to register an Inserter for a given resource type
    /// </summary>
    public interface IInserter<T> where T : class
    {
        /// <summary>
        /// The insert method for this IInserter instance. Defines the Insert
        /// operation for a given resource.
        /// </summary>
        int Insert(IEnumerable<T> entities, IRequest<T> request);
    }

    /// <summary>
    /// Interface used to register an Updater for a given resource type
    /// </summary>
    public interface IUpdater<T> where T : class
    {
        /// <summary>
        /// The update method for this IUpdater instance. Defines the Update
        /// operation for a given resource.
        /// </summary>
        int Update(IEnumerable<T> entities, IRequest<T> request);
    }

    /// <summary>
    /// Interface used to register a Deleter for a given resource type
    /// </summary>
    public interface IDeleter<T> where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        int Delete(IEnumerable<T> entities, IRequest<T> request);
    }
}