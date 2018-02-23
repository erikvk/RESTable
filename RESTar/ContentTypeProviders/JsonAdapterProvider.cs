using System.Collections.Generic;
using System.IO;
using RESTar.Results.Error.BadRequest;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// The <see cref="JsonAdapterProvider" /> simplifies the process of building a RESTar 
    /// content provider. Deserialization is usually the hardest part, and the JsonAdapterProvider
    /// uses JSON for all deserialization. All we need to do when subclassing the JsonAdapterProvider
    /// class is to provide a method for producing JSON from a byte array of some content type, and 
    /// a Serialize implementation.
    /// </summary>
    public abstract class JsonAdapterProvider : IContentTypeProvider
    {
        private readonly JsonContentProvider JsonProvider = new JsonContentProvider();

        /// <inheritdoc />
        public abstract ContentType[] CanWrite();

        /// <inheritdoc />
        public abstract ContentType[] CanRead();

        /// <inheritdoc />
        public abstract string GetContentDispositionFileExtension(ContentType contentType);

        /// <summary>
        /// Produces JSON that is then used to deserialize to entities of the resource type.
        /// Include true in the isSingularEntity property when the produced JSON encodes a single 
        /// entity (as opposed to an array of objects).
        /// </summary>
        protected abstract byte[] ProduceJson(byte[] body, out bool isSingularEntity);

        /// <inheritdoc />
        public T DeserializeEntity<T>(ContentType contentType, byte[] body) where T : class
        {
            var jsonbytes = ProduceJson(body, out var singular);
            if (singular)
                return JsonProvider.DeserializeEntity<T>("application/json", jsonbytes);
            throw new InvalidInputCount();
        }

        /// <inheritdoc />
        public abstract Stream SerializeEntity<T>(ContentType accept, T entity, IRequest request) where T : class;

        /// <inheritdoc />
        public abstract Stream SerializeCollection<T>(ContentType accept, IEnumerable<T> entities, IRequest request, out ulong entityCount)
            where T : class;

        /// <inheritdoc />
        public List<T> DeserializeCollection<T>(ContentType contentType, byte[] body) where T : class
        {
            var jsonbytes = ProduceJson(body, out var singular);
            if (singular)
                return new List<T> {JsonProvider.DeserializeEntity<T>("application/json", jsonbytes)};
            return JsonProvider.DeserializeCollection<T>("application/json", jsonbytes);
        }

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(ContentType contentType, IEnumerable<T> entities, byte[] body) where T : class
        {
            var json = ProduceJson(body, out var singular);
            if (!singular) throw new InvalidInputCount();
            return JsonProvider.Populate("application/json", entities, json);
        }
    }
}