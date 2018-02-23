using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RESTar.Serialization;
using RESTar.Serialization.NativeProtocol;
using static Newtonsoft.Json.Formatting;
using static RESTar.Admin.Settings;

namespace RESTar.ContentTypeProviders
{
    /// <inheritdoc />
    public class JsonContentProvider : IContentTypeProvider
    {
        /// <summary>
        /// The settings that are used when serializing and deserializing JSON
        /// </summary>
        private static JsonSerializerSettings Settings { get; }

        /// <summary>
        /// UTF 8 encoding without byte order mark (BOM)
        /// </summary>
        private static Encoding UTF8 { get; }

        /// <summary>
        /// The JSON serializer
        /// </summary>
        private static JsonSerializer Serializer { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTarSpecific = "application/restar-json";
        private const string Brief = "json";

        static JsonContentProvider()
        {
            Settings = new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DefaultResolver(),
                NullValueHandling = NullValueHandling.Include,
                FloatParseHandling = FloatParseHandling.Decimal,
            };
            var enumConverter = new StringEnumConverter();
            var headersConverter = new HeadersConverter();
            var ddictionaryConverter = new DDictionaryConverter();
            Settings.Converters.Add(enumConverter);
            Settings.Converters.Add(headersConverter);
            Settings.Converters.Add(ddictionaryConverter);
            UTF8 = RESTarConfig.DefaultEncoding;
            Serializer = JsonSerializer.Create(Settings);
        }

        #region Internals

        internal string SerializeFormatter(JToken formatterToken, out int indents)
        {
            using (var sw = new StringWriter())
            using (var jwr = new FormatWriter(sw))
            {
                jwr.Formatting = Indented;
                Serializer.Serialize(jwr, formatterToken);
                indents = jwr.Depth;
                return sw.ToString();
            }
        }

        internal string Serialize(object value, Formatting? formatting = null, Type type = null)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            return JsonConvert.SerializeObject(value, type, _formatting, Settings);
        }

        internal void Populate(JToken value, object target)
        {
            if (value == null || target == null) return;
            using (var sr = value.CreateReader())
                Serializer.Populate(sr, target);
        }

        internal MemoryStream SerializeStream(object entity, Formatting? formatting = null, Type type = null)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                jwr.Formatting = _formatting;
                Serializer.Serialize(jwr, entity);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #endregion


        #region IContentTypeProvider

        /// <inheritdoc />
        public string GetContentDispositionFileExtension(ContentType contentType) => ".json";

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(ContentType contentType, IEnumerable<T> entities, byte[] body) where T : class
        {
            var json = Encoding.UTF8.GetString(body);
            return entities.Select(item =>
            {
                JsonConvert.PopulateObject(json, item, Settings);
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
        public byte[] SerializeToBytes<T>(T obj)
        {
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                jwr.Formatting = _PrettyPrint ? Indented : None;
                Serializer.Serialize(jwr, obj);
            }
            return stream.ToArray();
        }

        /// <inheritdoc />
        public Stream SerializeEntity<T>(ContentType accept, T entity, IRequest request) where T : class
        {
            ulong entityCount;
            var stream = new MemoryStream();
            var formatter = request.MetaConditions.Formatter;
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                jwr.Formatting = _PrettyPrint ? Indented : None;
                swr.Write(formatter.Pre);
                Serializer.Serialize(jwr, entity);
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
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                jwr.Formatting = _PrettyPrint ? Indented : None;
                swr.Write(formatter.Pre);
                Serializer.Serialize(jwr, entities);
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
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
                return Serializer.Deserialize<T>(jsonReader);
        }

        /// <inheritdoc />
        public List<T> DeserializeCollection<T>(ContentType contentType, byte[] body) where T : class
        {
            using (var jsonStream = new MemoryStream(body))
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                jsonReader.Read();
                if (jsonReader.TokenType == JsonToken.StartObject)
                    return new List<T> {Serializer.Deserialize<T>(jsonReader)};
                return Serializer.Deserialize<List<T>>(jsonReader);
            }
        }

        /// <summary>
        /// Handle this JsonContentProvider as a JSON.net JsonSerializer
        /// </summary>
        public static implicit operator JsonSerializer(JsonContentProvider jsonContentProvider) => Serializer;

        #endregion
    }
}