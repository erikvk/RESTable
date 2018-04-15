using System.Collections.Generic;
using System.IO;
using System.Text;
using RESTar.Results;

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

        /// <summary>
        /// The RESTar default UTF8 encoding. An UTF8 encoding without BOM.
        /// </summary>
        protected static readonly Encoding UTF8 = RESTarConfig.DefaultEncoding;

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <inheritdoc />
        public abstract ContentType ContentType { get; }

        /// <inheritdoc />
        public abstract string[] MatchStrings { get; set; }

        /// <inheritdoc />
        public abstract bool CanRead { get; }

        /// <inheritdoc />
        public abstract bool CanWrite { get; }

        /// <inheritdoc />
        public abstract string ContentDispositionFileExtension { get; }

        /// <summary>
        /// Produces JSON that is then used to deserialize to entities of the resource type.
        /// Include true in the isSingularEntity property when the produced JSON encodes a single 
        /// entity (as opposed to an array of objects).
        /// </summary>
        protected abstract byte[] ProduceJson(byte[] body, out bool isSingularEntity);

        /// <inheritdoc />
        public T DeserializeEntity<T>(byte[] body) where T : class
        {
            var jsonbytes = ProduceJson(body, out var singular);
            if (singular)
                return JsonProvider.DeserializeEntity<T>(jsonbytes);
            throw new InvalidInputCount();
        }

        /// <inheritdoc />
        public abstract void SerializeEntity(object entity, Stream stream, IRequest request, out ulong entityCount);

        /// <inheritdoc />
        public abstract void SerializeCollection(IEnumerable<object> entities, Stream stream, IRequest request, out ulong entityCount);

        /// <inheritdoc />
        public IEnumerable<T> DeserializeCollection<T>(byte[] body) where T : class
        {
            var jsonbytes = ProduceJson(body, out var singular);
            if (singular)
                yield return JsonProvider.DeserializeEntity<T>(jsonbytes);
            else
                foreach (var item in JsonProvider.DeserializeCollection<T>(jsonbytes))
                    yield return item;
        }

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class
        {
            var json = ProduceJson(body, out var singular);
            if (!singular) throw new InvalidInputCount();
            return JsonProvider.Populate(entities, json);
        }
    }
}