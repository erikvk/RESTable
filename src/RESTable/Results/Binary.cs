using System.IO;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Holds a binary content, used as result from requests to binary resources
    /// </summary>
    public sealed class Binary : Content
    {
        private ISerializedResult SerializedResult { get; }

        public Stream Stream => SerializedResult.Body;

        /// <inheritdoc />
        public Binary(IRequest request, ContentType contentType) : base(request)
        {
            Headers.ContentType = contentType;
            SerializedResult = new SerializedResult(this);
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Binary)};{Request.Resource};";
    }
}