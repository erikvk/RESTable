using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RESTar.Admin;
using RESTar.Serialization;
using RESTar.Serialization.NativeProtocol;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    public class JsonContentProvider : IContentTypeProvider
    {
        private const string JsonMimeType = "application/json";
        private const string RESTarSpecific = "application/restar-json";
        private const string Brief = "json";

        /// <inheritdoc />
        public string GetContentDispositionFileExtension(ContentType contentType) => ".json";

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(ContentType contentType, IEnumerable<T> entities, byte[] body) where T : class
        {
            var json = Encoding.UTF8.GetString(body);
            return entities.Select(item =>
            {
                JsonConvert.PopulateObject(json, item, Serializer.Settings);
                return item;
            });
        }

        /// <inheritdoc />
        public ContentType[] CanWrite() => new ContentType[] {JsonMimeType, RESTarSpecific, Brief};

        /// <inheritdoc />
        public ContentType[] CanRead() => new ContentType[] {JsonMimeType, RESTarSpecific, Brief};

        /// <summary>
        /// Serializes the given object to a byte array
        /// </summary>
        public static byte[] SerializeToBytes<T>(T obj)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, Serializer.UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                Serializer.Json.Formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
                Serializer.Json.Serialize(jwr, obj);
            }
            return stream.ToArray();
        }

        /// <inheritdoc />
        public Stream SerializeEntity<T>(ContentType accept, T entity, IRequest request) where T : class
        {
            ulong entityCount;
            var stream = new MemoryStream();
            var formatter = request.MetaConditions.Formatter;
            using (var swr = new StreamWriter(stream, Serializer.UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                Serializer.Json.Formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
                swr.Write(formatter.Pre);
                Serializer.Json.Serialize(jwr, entity);
                entityCount = jwr.ObjectsWritten;
                swr.Write(formatter.Post);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return entityCount > 0 ? stream : null;
        }

        /// <inheritdoc />
        public Stream SerializeCollection<T>(ContentType accept, IEnumerable<T> entities, IRequest request, out ulong entityCount) where T : class
        {
            entityCount = 0;
            var stream = new MemoryStream();
            var formatter = request.MetaConditions.Formatter;
            using (var swr = new StreamWriter(stream, Serializer.UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                Serializer.Json.Formatting = Settings._PrettyPrint ? Formatting.Indented : Formatting.None;
                swr.Write(formatter.Pre);
                Serializer.Json.Serialize(jwr, entities);
                entityCount = jwr.ObjectsWritten;
                swr.Write(formatter.Post);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return entityCount > 0 ? stream : null;
        }

        /// <inheritdoc />
        public T DeserializeEntity<T>(ContentType contentType, byte[] body) where T : class
        {
            using (var jsonStream = new MemoryStream(body))
            using (var streamReader = new StreamReader(jsonStream, Serializer.UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
                return Serializer.Json.Deserialize<T>(jsonReader);
        }

        /// <inheritdoc />
        public List<T> DeserializeCollection<T>(ContentType contentType, byte[] body) where T : class
        {
            using (var jsonStream = new MemoryStream(body))
            using (var streamReader = new StreamReader(jsonStream, Serializer.UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                jsonReader.Read();
                if (jsonReader.TokenType == JsonToken.StartObject)
                    return new List<T> {Serializer.Json.Deserialize<T>(jsonReader)};
                return Serializer.Json.Deserialize<List<T>>(jsonReader);
            }
        }
    }
}