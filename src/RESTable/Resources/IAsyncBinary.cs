using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources
{
    public interface IBinaryInternal { }

    /// <summary>
    /// Defines the operations of a binary resource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IAsyncBinary<T> : IBinaryInternal where T : class, IAsyncBinary<T>
    {
        /// <summary>
        /// Generates a binary stream and content type for a request asynchronously
        /// </summary>
        ValueTask<BinaryResult> SelectAsync(IRequest<T> request, CancellationToken cancellationToken);
    }
}