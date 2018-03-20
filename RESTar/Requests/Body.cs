using System.Collections.Generic;
using RESTar.Serialization;

namespace RESTar.Requests
{
    /// <summary>
    /// Encodes a request body
    /// </summary>
    public struct Body
    {
        private IContentTypeProvider ContentTypeProvider { get; }

        /// <summary>
        /// The body's bytes
        /// </summary>
        public byte[] Bytes { get; }

        /// <summary>
        /// Deserializes the body to a list of entitites of the given type
        /// </summary>
        public List<T> ToList<T>() where T : class => HasContent ? ContentTypeProvider.DeserializeCollection<T>(Bytes) : null;

        /// <summary>
        /// Populates the body onto each entity in a source collection
        /// </summary>
        public IEnumerable<T> PopulateTo<T>(IEnumerable<T> source) where T : class => ContentTypeProvider.Populate(source, Bytes);

        /// <summary>
        /// Does this Body have content?
        /// </summary>
        public bool HasContent { get; }

        internal Body(byte[] bytes, ContentType contentType, IContentTypeProvider contentTypeProvider)
        {
            ContentTypeProvider = contentTypeProvider;
            Bytes = bytes;
            HasContent = bytes?.Length > 0;
        }

        /// <summary>
        /// Creates a new Body instance from a JSON serializable .NET object.
        /// </summary>
        /// <param name="content"></param>
        public Body(object content)
        {
            Bytes = content != null ? Serializers.Json.SerializeToBytes(content) : new byte[0];
            ContentTypeProvider = Serializers.Json;
            HasContent = Bytes?.Length > 0;
        }
    }
}