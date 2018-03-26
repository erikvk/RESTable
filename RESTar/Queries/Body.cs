using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Results.Error;
using RESTar.Results.Error.NotFound;
using RESTar.Serialization;

namespace RESTar.Queries
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
        public byte[] Bytes { get; }

        /// <summary>
        /// Deserializes the body to a list of entitites of the given type
        /// </summary>
        public List<T> ToList<T>() where T : class
        {
            if (!HasContent) return null;
            var contentTypeProvider = ProtocolProvider.InputMimeBindings.SafeGet(ContentType.MimeType) ??
                                      throw new UnsupportedContent(ContentType.MimeType);
            return contentTypeProvider.DeserializeCollection<T>(Bytes);
        }

        /// <summary>
        /// Populates the body onto each entity in a source collection. If the body is empty,
        /// returns null.
        /// </summary>
        public IEnumerable<T> PopulateTo<T>(IEnumerable<T> source) where T : class
        {
            if (!HasContent) return null;
            var contentTypeProvider = ProtocolProvider.InputMimeBindings.SafeGet(ContentType.MimeType) ??
                                      throw new UnsupportedContent(ContentType.MimeType);
            return contentTypeProvider.Populate(source, Bytes);
        }

        /// <summary>
        /// Does this Body have content?
        /// </summary>
        public bool HasContent { get; }

        internal Body(byte[] bytes, ContentType contentType, CachedProtocolProvider protocolProvider)
        {
            ContentType = contentType;
            Bytes = bytes;
            HasContent = bytes?.Length > 0;
            ProtocolProvider = protocolProvider;
        }

        /// <summary>
        /// Creates a new Body instance from a JSON serializable .NET object.
        /// </summary>
        /// <param name="content"></param>
        public Body(object content)
        {
            Bytes = content != null ? Serializers.Json.SerializeToBytes(content) : new byte[0];
            ContentType = Serializers.Json.ContentType;
            HasContent = Bytes?.Length > 0;
            ProtocolProvider = ProtocolController.DefaultProtocolProvider;
        }

        /// <summary>
        /// Creates a new body from a byte array
        /// </summary>
        /// <param name="bytes">The bytes that constitute the body</param>
        /// <param name="protocolIdentifer">An optional protocol provider identifier used for specifying a protocol.
        /// If null, the default protocol is used.</param>
        /// <param name="contentType">An optional content type to use when deserializing the body. If null, the default 
        /// content type of the protocol is used.</param>
        public Body(byte[] bytes, string protocolIdentifer = null, ContentType? contentType = null)
        {
            Bytes = bytes;
            ProtocolProvider = protocolIdentifer == null
                ? ProtocolController.DefaultProtocolProvider
                : ProtocolController.ProtocolProviders.SafeGet(protocolIdentifer)
                  ?? throw new UnknownProtocol(protocolIdentifer);
            ContentType = contentType ?? ProtocolProvider.DefaultInputProvider.ContentType;
            HasContent = bytes?.Length > 0;
        }
    }
}