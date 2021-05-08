using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an Inserter for a given resource type
    /// </summary>
    public interface IAsyncInserter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The insert method for this IInserter instance. Defines the Insert
        /// operation for a given resource.
        /// </summary>
        IAsyncEnumerable<T> InsertAsync(IRequest<T> request);
    }
}