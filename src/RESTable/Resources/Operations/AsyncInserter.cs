using System.Collections.Generic;
using System.Threading;
using RESTable.Requests;

namespace RESTable.Resources.Operations;

/// <summary>
///     Specifies the Insert operation used in POST and PUT. Takes a set of entities and inserts
///     them into the resource, and returns the entities successfully inserted.
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
internal delegate IAsyncEnumerable<T> AsyncInserter<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class;
