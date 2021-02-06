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
        /// <inheritdoc />
        public Binary(IRequest request, ContentType contentType) : base(request)
        {
            Headers.ContentType = contentType;
            if (Body.CanSeek)
                Body.Seek(0, SeekOrigin.Begin);
            IsSerialized = true;
        }

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Binary)};{Request.Resource};";
    }
}