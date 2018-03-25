using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
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

        private static JsonSerializerSettings SettingsIgnoreNulls { get; }

        /// <summary>
        /// UTF 8 encoding without byte order mark (BOM)
        /// </summary>
        private static Encoding UTF8 { get; }

        /// <summary>
        /// The JSON serializer
        /// </summary>
        internal static JsonSerializer Serializer { get; }

        private static JsonSerializer SerializerIgnoreNulls { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTarSpecific = "application/restar-json";
        private const string Brief = "json";
        private const string TextPlain = "text/plain";

        static JsonContentProvider()
        {
            UTF8 = RESTarConfig.DefaultEncoding;

            Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DefaultResolver(),
                FloatParseHandling = FloatParseHandling.Decimal
            };
            Serializer = JsonSerializer.Create(Settings);

            SettingsIgnoreNulls = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = Settings.DateParseHandling,
                DateFormatHandling = Settings.DateFormatHandling,
                DateTimeZoneHandling = Settings.DateTimeZoneHandling,
                ContractResolver = Settings.ContractResolver,
                FloatParseHandling = Settings.FloatParseHandling
            };
            SerializerIgnoreNulls = JsonSerializer.Create(SettingsIgnoreNulls);
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

        internal void PopulateJToken(JToken value, object target)
        {
            if (value == null || target == null) return;
            using (var sr = value.CreateReader())
                Serializer.Populate(sr, target);
        }

        internal void Populate(string json, object target)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JsonConvert.PopulateObject(json, target, Settings);
        }

        internal MemoryStream SerializeStream(object entity, Formatting? formatting = null, bool ignoreNulls = false)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var serializer = ignoreNulls ? SerializerIgnoreNulls : Serializer;
            var stream = new MemoryStream();
            using (var swr = new StreamWriter(stream, UTF8, 1024, true))
            using (var jwr = new RESTarJsonWriter(swr, 0))
            {
                jwr.Formatting = _formatting;
                serializer.Serialize(jwr, entity);
            }
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        #endregion

        /// <summary>
        /// Serializes the value to the given JsonTextWriter
        /// </summary>
        public void Serialize(JsonTextWriter jsonWriter, object value)
        {
            Serializer.Serialize(jsonWriter, value);
        }

        /// <summary>
        /// Serializes the given value using the formatting, and optionally - a type. If no 
        /// formatting is given, the formatting defined in Settings is used.
        /// </summary>
        public string Serialize(object value, Formatting? formatting = null, bool ignoreNulls = false)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var settings = ignoreNulls ? SettingsIgnoreNulls : Settings;
            return JsonConvert.SerializeObject(value, _formatting, settings);
        }

        /// <summary>
        /// Deserializes the given json string to the given type.
        /// </summary>
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        #region IContentTypeProvider

        /// <inheritdoc />
        public string Name => "JSON";

        /// <inheritdoc />
        public ContentType ContentType { get; } = new ContentType("application/json; charset=utf-8");

        /// <inheritdoc />
        public bool CanRead => true;

        /// <inheritdoc />
        public bool CanWrite => true;

        /// <inheritdoc />
        public string ContentDispositionFileExtension => ".json";

        /// <inheritdoc />
        public IEnumerable<T> Populate<T>(IEnumerable<T> entities, byte[] body) where T : class
        {
            var json = Encoding.UTF8.GetString(body);
            return entities.Select(item =>
            {
                JsonConvert.PopulateObject(json, item, Settings);
                return item;
            });
        }

        /// <inheritdoc />
        public string[] MatchStrings { get; set; } = {JsonMimeType, RESTarSpecific, Brief, TextPlain};

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
        public void SerializeEntity<T>(T entity, Stream stream, IQuery query, out ulong entityCount) where T : class
        {
            if (entity == null)
            {
                entityCount = 0;
                return;
            }
            var formatter = query.MetaConditions.Formatter ?? DbOutputFormat.Default;
            using (var swr = new StreamWriter(stream, UTF8, 2048, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                jwr.Formatting = _PrettyPrint ? Indented : None;
                swr.Write(formatter.Pre);
                Serializer.Serialize(jwr, entity);
                entityCount = jwr.ObjectsWritten;
                swr.Write(formatter.Post);
            }
            stream.Seek(0, SeekOrigin.Begin);
        }

        /// <inheritdoc />
        public void SerializeCollection<T>(IEnumerable<T> entities, Stream stream, IQuery query, out ulong entityCount) where T : class
        {
            if (entities == null)
            {
                entityCount = 0;
                return;
            }
            var formatter = query.MetaConditions.Formatter ?? DbOutputFormat.Default;
            using (var swr = new StreamWriter(stream, UTF8, 2048, true))
            using (var jwr = new RESTarJsonWriter(swr, formatter.StartIndent))
            {
                jwr.Formatting = _PrettyPrint ? Indented : None;
                swr.Write(formatter.Pre);
                Serializer.Serialize(jwr, entities);
                entityCount = jwr.ObjectsWritten;
                swr.Write(formatter.Post);
            }
            stream.Seek(0, SeekOrigin.Begin);
        }

        /// <inheritdoc />
        public T DeserializeEntity<T>(byte[] body) where T : class
        {
            using (var jsonStream = new MemoryStream(body))
            using (var streamReader = new StreamReader(jsonStream, UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
                return Serializer.Deserialize<T>(jsonReader);
        }

        /// <inheritdoc />
        public List<T> DeserializeCollection<T>(byte[] body) where T : class
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

        #endregion
    }
}