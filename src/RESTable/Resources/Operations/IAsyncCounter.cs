using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Counter for a given resource type
    /// </summary>
    public interface IAsyncCounter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        ValueTask<long> CountAsync(IRequest<T> request, CancellationToken cancellationToken);
    }
}