using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register a Deleter for a given resource type
    /// </summary>
    public interface IAsyncDeleter<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The delete method for this IDeleter instance. Defines the Delete
        /// operation for a given resource.
        /// </summary>
        ValueTask<int> DeleteAsync(IRequest<T> request, CancellationToken cancellationToken);
    }
}