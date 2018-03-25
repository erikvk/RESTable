using System.Collections.Generic;
using RESTar.Admin;
using RESTar.Operations;

namespace RESTar
{
    /// <summary>
    /// A common interface for all operation interfaces
    /// </summary>
    public interface IOperationsInterface { }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Selector for a given resource type
    /// </summary>
    public interface ISelector<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The select method for this ISelector instance. Defines the Select
        /// operation for a given resource.
        /// </summary>
        IEnumerable<T> Select(IQuery<T> query);
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
        int Insert(IQuery<T> query);
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
        int Update(IQuery<T> query);
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
        int Delete(IQuery<T> query);
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
        long Count(IQuery<T> query);
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
        ResourceProfile Profile(IQuery<T> query);
    }

    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an authenticator for a given resource type.
    /// Authenticators are executed once for each REST request to this resource.
    /// </summary>
    public interface IAuthenticatable<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        AuthResults Authenticate(IQuery<T> query);
    }
}