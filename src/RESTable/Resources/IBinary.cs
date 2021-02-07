using System.IO;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources
{
    /// <summary>
    /// Defines the operations of a binary resource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBinary<T> where T : class, IBinary<T>
    {
        /// <summary>
        /// Generates a binary stream and content type for a request
        /// </summary>
        Task<(Stream stream, ContentType contentType)> SelectAsync(IRequest<T> request);
    }
}