using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// Interface used to register an Updater for a given resource type
    /// </summary>
    public interface IAsyncUpdater<T> : IOperationsInterface where T : class
    {
        /// <summary>
        /// The update method for this IUpdater instance. Defines the Update
        /// operation for a given resource.
        /// </summary>
        Task<int> UpdateAsync(IRequest<T> request);
    }
}