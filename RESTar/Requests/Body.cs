using System.Collections.Generic;
using System.IO;
using RESTar.Internal;
using RESTar.Results;

namespace RESTar.Requests
{
    /// <summary>
    /// Encodes a request body
    /// </summary>
    public struct Body
    {
        /// <summary>
        /// The content type of the body
        /// </summary>
        public ContentType ContentType { get; }

        private CachedProtocolProvider ProtocolProvider { get; }

        /// <summary>
        /// The body's bytes
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Deserializes the body to an IEnumerable of entities of the given type
        /// </summary>
        public IEnumerable<T> Deserialize<T>() where T : class
        {
            if (!HasContent) return null;
            var contentTypeProvider = ProtocolProvider.InputMimeBindings.SafeGet(ContentType.MediaType) ??
                                      throw new UnsupportedContent(ContentType.MediaType);
            if (Stream.CanSeek)
                Stream.Seek(0, SeekOrigin.Begin);
            return contentTypeProvider.DeserializeCollection<T>(Stream);
        }

        /// <summary>
        /// Populates the body onto each entity in a source collection. If the body is empty,
        /// returns null.
        /// </summary>
        public IEnumerable<T> PopulateTo<T>(IEnumerable<T> source) where T : class
        {
            if (source == null || !HasContent) return null;
            var contentTypeProvider = ProtocolProvider.InputMimeBindings.SafeGet(ContentType.MediaType) ??
                                      throw new UnsupportedContent(ContentType.MediaType);
            var buffer = Stream.ToByteArray();
            return contentTypeProvider.Populate(source, buffer);
        }

        /// <summary>
        /// Does this Body have content?
        /// </summary>
        public bool HasContent { get; }

        internal string LengthLogString
        {
            get
            {
                if (!HasContent || !Stream.CanSeek) return "";
                return $" ({Stream.Length} bytes)";
            }
        }

        internal Body(Stream stream, ContentType contentType, CachedProtocolProvider protocolProvider)
        {
            ContentType = contentType;
            Stream = stream;
            if (stream == null)
                HasContent = false;
            else if (stream.CanSeek)
                HasContent = stream.Length > 0;
            else HasContent = true;
            ProtocolProvider = protocolProvider;
        }

        private const int MaxStringLength = 50_000;

        /// <inheritdoc />
        public override string ToString()
        {
            if (!HasContent || !Stream.CanSeek) return "";
            string str;
            Stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(Stream, RESTarConfig.DefaultEncoding, false, 1024, true);
            if (Stream.Length > MaxStringLength)
            {
                var buffer = new char[MaxStringLength];
                using (reader) reader.Read(buffer, 0, buffer.Length);
                str = new string(buffer);
            }
            else
            {
                using (reader) str = reader.ReadToEnd();
            }
            Stream.Seek(0, SeekOrigin.Begin);
            return str;
        }
    }
}