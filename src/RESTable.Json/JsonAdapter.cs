using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.Json
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

        protected IJsonProvider JsonProvider { get; }

        protected JsonAdapter(IJsonProvider jsonProvider)
        {
            JsonProvider = jsonProvider;
        }

        /// <summary>
        /// Produces JSON that is then used to deserialize to entities of the resource type. The
        /// input stream contains data from the client, in the format of the content type provider.
        /// Read this data and write corresponding JSON to the given output stream.
        /// Include true in the isSingularEntity property when the produced JSON encodes a single 
        /// entity (as opposed to an array of entities).
        /// </summary>
        protected abstract Task ProduceJsonArrayAsync(Stream inputStream, Stream outputStream);

        /// <inheritdoc />
        public abstract ValueTask<long> SerializeCollectionAsync<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken);

        /// <inheritdoc />
        public abstract Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken);

        /// <inheritdoc />
        public async IAsyncEnumerable<T?> DeserializeCollection<T>(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var jsonStream = new SwappingStream();
            await using (jsonStream.ConfigureAwait(false))
            {
                await ProduceJsonArrayAsync(stream, jsonStream).ConfigureAwait(false);
                await foreach (var item in JsonProvider.DeserializeCollection<T>(jsonStream.Rewind(), cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var jsonStream = new SwappingStream();
            await using (jsonStream.ConfigureAwait(false))
            {
                var populateStream = new MemoryStream(body);
#if NETSTANDARD2_0
                using (populateStream)
#else
                await using (populateStream.ConfigureAwait(false))
#endif
                {
                    await ProduceJsonArrayAsync(populateStream, jsonStream).ConfigureAwait(false);
                    await foreach (var item in JsonProvider.Populate(entities, await jsonStream.GetBytesAsync(), cancellationToken).ConfigureAwait(false))
                        yield return item;
                }
            }
        }
    }
}