using System.IO;

namespace RESTar
{
    /// <summary>
    /// Defines the operations of bucket resources
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBucket<T> where T : class
    {
        /// <summary>
        /// Generates a binary stream and content type for a request
        /// </summary>
        (Stream stream, ContentType contentType) Select(IRequest<T> request);
    }
}