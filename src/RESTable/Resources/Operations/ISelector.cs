using System.Collections.Generic;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <inheritdoc />
/// <summary>
///     Interface used to register a Selector for a given resource type
/// </summary>
public interface ISelector<T> : IOperationsInterface where T : class
{
    /// <summary>
    ///     The select method for this ISelector instance. Defines the Select
    ///     operation for a given resource.
    /// </summary>
    IEnumerable<T> Select(IRequest<T> request);
}