using System.Collections.Generic;
using System.IO;

namespace RESTar
{
    /// <summary>
    /// Defines the operations ofa content type provider, that is used when 
    /// finalizing results to a given content type.
    /// </summary>
    public interface IContentTypeProvider
    {
        /// <summary>
        /// The content type that is handled by this content type provider
        /// </summary>
        /// <returns></returns>
        ContentType ContentType { get; }

        /// <summary>
        /// The strings that should be registered as match strings for this content type provider. When 
        /// these are used as MIME types in request headers, they will map to this content type provider. 
        /// </summary>
        string[] MatchStrings { get; }

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
        /// Serializes the entity to the given content type. Serialize calls can only be made with 
        /// content types included in CanWrite.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        Stream SerializeEntity<T>(T entity, IRequest request) where T : class;

        /// <summary>
        /// Serializes the entity collection to the given content type. Serialize calls can only be made with 
        /// content types included in CanWrite. Include the number of entities serialized in the entityCount 
        /// out parameter.
        /// </summary>
        Stream SerializeCollection<T>(IEnumerable<T> entities, IRequest request, out ulong entityCount) where T : class;

        /// <summary>
        /// Deserializes the byte array to the given content entity type. Deserialize calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        T DeserializeEntity<T>(byte[] body) where T : class;

        /// <summary>
        /// Deserializes the byte array to the given content entity collection type. Deserialize calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        List<T> DeserializeCollection<T>(byte[] body) where T : class;

        /// <summary>
        /// Populates the byte array to all entities in the given collection. Populate calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class;
    }
}