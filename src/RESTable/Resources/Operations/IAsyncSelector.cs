using System.Collections.Generic;
using System.Threading;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <inheritdoc />
/// <summary>
///     Interface used to register a Selector for a given resource type
/// </summary>
public interface IAsyncSelector<T> : IOperationsInterface where T : class
{
    /// <summary>
    ///     The select method for this ISelector instance. Defines the Select
    ///     operation for a given resource.
    /// </summary>
    IAsyncEnumerable<T> SelectAsync(IRequest<T> request, CancellationToken cancellationToken);
}
