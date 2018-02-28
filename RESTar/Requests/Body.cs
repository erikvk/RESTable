using System.Collections.Generic;

namespace RESTar.Requests
{
    /// <summary>
    /// Encodes a request body
    /// </summary>
    public struct Body
    {
        private ContentType ContentType { get; }
        private IContentTypeProvider ContentTypeProvider { get; }

        /// <summary>
        /// The body's bytes
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// Deserializes the body to a list of entitites of the given type
        /// </summary>
        public List<T> ToList<T>() where T : class => HasContent
            ? ContentTypeProvider.DeserializeCollection<T>(ContentType, Bytes)
            : null;

        /// <summary>
        /// Populates the body onto each entity in a source collection
        /// </summary>
        public IEnumerable<T> PopulateTo<T>(IEnumerable<T> source) where T : class => ContentTypeProvider.Populate(ContentType, source, Bytes);

        /// <summary>
        /// Does this Body have content?
        /// </summary>
        public bool HasContent { get; }

        internal Body(byte[] bytes, ContentType contentType, IContentTypeProvider contentTypeProvider)
        {
            ContentType = contentType;
            ContentTypeProvider = contentTypeProvider;
            Bytes = bytes;
            HasContent = bytes?.Length > 0;
        }
    }
}