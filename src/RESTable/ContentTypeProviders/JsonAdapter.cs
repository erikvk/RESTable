using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.ContentTypeProviders
{
    /// <inheritdoc />
    /// <summary>
    /// The <see cref="JsonAdapter" /> simplifies the process of building a RESTable 
    /// content provider. Deserialization is usually the hardest part, and the JsonAdapterProvider
    /// uses JSON for all deserialization. All we need to do when subclassing the JsonAdapterProvider
    /// class is to provide a method for producing JSON from a byte array of some content type, and 
    /// a Serialize implementation.
    /// </summary>
    public abstract class JsonAdapter : IContentTypeProvider
    {
        private readonly NewtonsoftJsonProvider JsonProvider = new();

        /// <summary>
        /// The RESTable default UTF8 encoding. An UTF8 encoding without BOM.
        /// </summary>
        protected static readonly Encoding UTF8 = RESTableConfig.DefaultEncoding;

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
        /// Produces JSON that is then used to deserialize to entities of the resource type. The
        /// input stream contains data from the client, in the format of the content type provider.
        /// Read this data and write corresponding JSON to the given output stream.
        /// Include true in the isSingularEntity property when the produced JSON encodes a single 
        /// entity (as opposed to an array of entities).
        /// </summary>
        protected abstract Task ProduceJsonArray(Stream inputStream, Stream outputStream);

        /// <inheritdoc />
        public abstract Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collection, Stream stream, IRequest request, CancellationToken cancellationToken) where T : class;

        /// <inheritdoc />
        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream stream)
        {
            await using var jsonStream = new SwappingStream();
            await ProduceJsonArray(stream, jsonStream).ConfigureAwait(false);
            await foreach (var item in JsonProvider.DeserializeCollection<T>(jsonStream.Rewind()).ConfigureAwait(false))
                yield return item;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body)
        {
            await using var jsonStream = new SwappingStream();
            await using var populateStream = new MemoryStream(body);
            await ProduceJsonArray(populateStream, jsonStream).ConfigureAwait(false);
            await foreach (var item in JsonProvider.Populate(entities, await jsonStream.GetBytesAsync()).ConfigureAwait(false))
                yield return item;
        }
    }
}