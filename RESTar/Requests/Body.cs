using System.Collections.Generic;
using RESTar.Internal;
using RESTar.Results.Error;

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
            if (source == null || !HasContent) return null;
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


    }
}