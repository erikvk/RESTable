using System.Collections.Generic;
using RESTar.Admin;

namespace RESTar
{
    /// <summary>
    /// A common interface for all operation interfaces
    /// </summary>
    public interface IOperationsInterface { }

    /// <summary>
    /// Interface used to register a Selector for a given resource type
    /// </summary>
    public interface ISelector<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The select method for this ISelector instance. Defines the Select
        /// operation for a given resource.
        /// </summary>
        IEnumerable<T> Select(IRequest<T> request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an Inserter for a given resource type
    /// </summary>
    public interface IInserter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The insert method for this IInserter instance. Defines the Insert
        /// operation for a given resource.
        /// </summary>
        int Insert(IEnumerable<T> entities, IRequest<T> request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an Updater for a given resource type
    /// </summary>
    public interface IUpdater<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The update method for this IUpdater instance. Defines the Update
        /// operation for a given resource.
        /// </summary>
        int Update(IEnumerable<T> entities, IRequest<T> request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Deleter for a given resource type
    /// </summary>
    public interface IDeleter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        int Delete(IEnumerable<T> entities, IRequest<T> request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Counter for a given resource type
    /// </summary>
    public interface ICounter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        long Count(IRequest<T> request);
    }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Profiler for a given resource type
    /// </summary>
    public interface IProfiler<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        ResourceProfile Profile(IRequest<T> request);
    }
}