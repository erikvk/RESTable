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
        /// Returns an array of all the content types that this provider can write to.
        /// </summary>
        /// <returns></returns>
        ContentType[] CanWrite();

        /// <summary>
        /// Returns an array of all the content types that this provider can read from.
        /// </summary>
        /// <returns></returns>
        ContentType[] CanRead();

        /// <summary>
        /// Returns the file extension to use with the given content type in content disposition
        /// headers and file attachments. For example ".docx".
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        string GetContentDispositionFileExtension(ContentType contentType);

        /// <summary>
        /// Serializes the entity to the given content type. Serialize calls can only be made with 
        /// content types included in CanWrite.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        Stream SerializeEntity<T>(ContentType accept, T entity, IRequest request) where T : class;

        /// <summary>
        /// Serializes the entity collection to the given content type. Serialize calls can only be made with 
        /// content types included in CanWrite. Include the number of entities serialized in the entityCount 
        /// out parameter.
        /// </summary>
        Stream SerializeCollection<T>(ContentType accept, IEnumerable<T> entities, IRequest request, out ulong entityCount) where T : class;

        /// <summary>
        /// Deserializes the byte array to the given content entity type. Deserialize calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        T DeserializeEntity<T>(ContentType contentType, byte[] body) where T : class;

        /// <summary>
        /// Deserializes the byte array to the given content entity collection type. Deserialize calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        List<T> DeserializeCollection<T>(ContentType contentType, byte[] body) where T : class;

        /// <summary>
        /// Populates the byte array to all entities in the given collection. Populate calls can only be made with 
        /// content types included in CanRead.
        /// </summary>
        IEnumerable<T> Populate<T>(ContentType contentType, IEnumerable<T> entities, byte[] body) where T : class;
    }
}