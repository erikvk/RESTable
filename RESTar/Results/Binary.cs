using System.IO;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Holds a binary content, used as result from requests to binary resources
    /// </summary>
    public class Binary : Content
    {
        /// <inheritdoc />
        public override Stream Body { get; set; }

        /// <inheritdoc />
        public Binary(IRequest request, Stream stream, ContentType contentType) : base(request)
        {
            Headers.ContentType = contentType;
            Body = stream;
            IsSerialized = true;
        }
    }
}