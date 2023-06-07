using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <inheritdoc />
/// <summary>
///     Interface used to register an Updater for a given resource type
/// </summary>
public interface IUpdater<T> : IOperationsInterface where T : class
{
    /// <summary>
    ///     The update method for this IUpdater instance. Defines the Update
    ///     operation for a given resource.
    /// </summary>
    IEnumerable<T> Update(IRequest<T> request);
}
