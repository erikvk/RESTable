using System.Collections.Generic;
using System.Threading;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Specifies the Update operation used in PATCH and PUT. Takes a set of entities and updates 
    /// their corresponding entities in the resource (often by deleting the old ones and inserting 
    /// the new), and returns the entities successfully updated.
    /// </summary>
    /// <typeparam name="T">The resource type</typeparam>
    internal delegate IAsyncEnumerable<T> AsyncUpdater<T>(IRequest<T> request, CancellationToken cancellationToken) where T : class;
}