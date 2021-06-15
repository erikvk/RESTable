using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.ContentTypeProviders
{
    /// <summary>
    /// Defines the operations of a content type provider, that is used when 
    /// serialize results to a given content type.
    /// </summary>
    public interface IContentTypeProvider
    {
        /// <summary>
        /// The name of the content type, used when listing available content types.
        /// For example, JSON.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The content type that is handled by this content type provider.
        /// </summary>
        /// <returns></returns>
        ContentType ContentType { get; }

        /// <summary>
        /// The strings that should be registered as match strings for this content type provider. When 
        /// these are used as MIME types in request headers, they will map to this content type provider.
        /// Protocol providers can change these in order to make custom mappings to content types.
        /// </summary>
        string[] MatchStrings { get; set; }

        /// <summary>
        /// Can this content type provider read data?
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// Can this content type provider write data?
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// Returns the file extension to use with the given content type in content disposition
        /// headers and file attachments. For example ".docx".
        /// </summary>
        string ContentDispositionFileExtension { get; }

        /// <summary>
        /// Serializes the entity collection to the given Stream and returns the number of entities serialized.
        /// </summary>
        Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collection, Stream stream, IRequest? request, CancellationToken cancellationToken) where T : class;

        /// <summary>
        /// Serializes the entity to the given Stream and returns the number of entities serialized.
        /// </summary>
        Task Serialize<T>(T item, Stream stream, IRequest? request, CancellationToken cancellationToken) where T : class;

        /// <summary>
        /// Deserializes the data from the stream to the given content entity collection type. Deserialize calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        IAsyncEnumerable<T> DeserializeCollection<T>(Stream stream);

        /// <summary>
        /// Populates the data from the byte array to all entities in the given collection. Populate calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body);
    }
}