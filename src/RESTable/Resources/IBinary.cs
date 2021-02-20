using System;
using System.IO;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources
{
    public class BinaryResult
    {
        public Func<Stream, Task> WriteToStream { get; }
        public ContentType ContentType { get; }

        public BinaryResult(Func<Stream, Task> writeToStream, ContentType contentType)
        {
            WriteToStream = writeToStream;
            ContentType = contentType;
        }
    }

    /// <summary>
    /// Defines the operations of a binary resource
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IBinary<T> where T : class, IBinary<T>
    {
        /// <summary>
        /// Generates a binary stream and content type for a request
        /// </summary>
        BinaryResult Select(IRequest<T> request);
    }
}