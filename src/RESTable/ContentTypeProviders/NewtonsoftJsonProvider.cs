﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders.NativeJsonProtocol;
using RESTable.Requests;
using static Newtonsoft.Json.Formatting;
using static RESTable.Admin.Settings;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, Stream stream, int baseIndentation, IRequest request = null) where T : class;
    }

    /// <inheritdoc cref="RESTable.ContentTypeProviders.IJsonProvider" />
    /// <inheritdoc cref="RESTable.ContentTypeProviders.IContentTypeProvider" />
    public class NewtonsoftJsonProvider : IJsonProvider, IContentTypeProvider
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

        /// <summary>
        /// Creates a new JSON.net JsonSerializer with the current RESTable serialization settings
        /// </summary>
        /// <returns></returns>
        public static JsonSerializer GetSerializer() => JsonSerializer.Create(Settings);

        private static JsonSerializer SerializerIgnoreNulls { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTableSpecific = "application/restable-json";
        private const string Brief = "json";
        private const string TextPlain = "text/plain";

        static NewtonsoftJsonProvider()
        {
            UTF8 = RESTableConfig.DefaultEncoding;
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

        /// <summary>
        /// Creates a new instance of the <see cref="NewtonsoftJsonProvider"/> type
        /// </summary>
        public NewtonsoftJsonProvider()
        {
            MatchStrings = new[] {JsonMimeType, RESTableSpecific, Brief, TextPlain};
            ContentDispositionFileExtension = ".json";
            CanWrite = true;
            CanRead = true;
            ContentType = "application/json; charset=utf-8";
            Name = "JSON";
        }

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

        /// <summary>
        /// A general-purpose serializer. Serializes the given value using the formatting, and optionally - a type. If no 
        /// formatting is given, the formatting defined in Settings is used.
        /// </summary>
        public string Serialize(object value, Formatting? formatting = null, bool ignoreNulls = false)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var settings = ignoreNulls ? SettingsIgnoreNulls : Settings;
            return JsonConvert.SerializeObject(value, _formatting, settings);
        }

        /// <summary>
        /// A general-purpose deserializer. Deserializes the given JSON string.
        /// </summary>
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>
        /// A general-purpose deserializer. Deserializes the given byte array.
        /// </summary>
        public T Deserialize<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }

        /// <summary>
        /// Serializes the value to the given JsonTextWriter, using the default serializer
        /// </summary>
        public void Serialize(JsonTextWriter jsonWriter, object value)
        {
            Serializer.Serialize(jsonWriter, value);
        }

        /// <summary>
        /// Populates JSON data onto an object
        /// </summary>
        public void Populate(string json, object target)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JsonConvert.PopulateObject(json, target, Settings);
        }

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <returns></returns>
        public void SerializeToStream(Stream stream, object entity, Formatting? formatting = null, bool ignoreNulls = false)
        {
            var _formatting = formatting ?? (_PrettyPrint ? Indented : None);
            var serializer = ignoreNulls ? SerializerIgnoreNulls : Serializer;
            using var swr = new StreamWriter(stream, UTF8, 1024, true);
            using var jwr = new RESTableJsonWriter(swr, 0) {Formatting = _formatting};
            serializer.Serialize(jwr, entity);
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public ContentType ContentType { get; }

        /// <inheritdoc />
        public bool CanRead { get; }

        /// <inheritdoc />
        public bool CanWrite { get; }

        /// <inheritdoc />
        public string ContentDispositionFileExtension { get; }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            await foreach (var entity in entities.ConfigureAwait(false))
            {
                JsonConvert.PopulateObject(json, entity, Settings);
                yield return entity;
            }
        }

        /// <inheritdoc />
        public string[] MatchStrings { get; set; }

        /// <inheritdoc />
        public async Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collection, Stream stream, IRequest request = null) where T : class
        {
            if (collection == null) return 0;
            await using var swr = new StreamWriter
            (
                stream: stream,
                encoding: UTF8,
                bufferSize: 2048,
                leaveOpen: true
            );
            using var jwr = new RESTableJsonWriter(swr, 0)
            {
                Formatting = _PrettyPrint ? Indented : None
            };
            Serializer.Serialize(jwr, collection.ToEnumerable());
            return jwr.ObjectsWritten;
        }

        /// <inheritdoc />
        public async Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, Stream stream, int baseIndentation, IRequest request = null) where T : class
        {
            if (collectionObject == null) return 0;
            await using var swr = new StreamWriter
            (
                stream: stream,
                encoding: UTF8,
                bufferSize: 2048,
                leaveOpen: true
            );
            using var jwr = new RESTableJsonWriter(swr, baseIndentation)
            {
                Formatting = _PrettyPrint ? Indented : None
            };
            Serializer.Serialize(jwr, collectionObject.ToEnumerable());
            return jwr.ObjectsWritten;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream body)
        {
            using var streamReader = new StreamReader
            (
                stream: body,
                encoding: UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024,
                leaveOpen: true
            );
            using var jsonReader = new JsonTextReader(streamReader);
            await jsonReader.ReadAsync().ConfigureAwait(false);
            switch (jsonReader.TokenType)
            {
                case JsonToken.None: yield break;
                case JsonToken.StartObject:
                    yield return Serializer.Deserialize<T>(jsonReader);
                    break;
                case JsonToken.StartArray:
                    await jsonReader.ReadAsync().ConfigureAwait(false);
                    while (jsonReader.TokenType != JsonToken.EndArray)
                    {
                        yield return Serializer.Deserialize<T>(jsonReader);
                        await jsonReader.ReadAsync().ConfigureAwait(false);
                    }
                    break;
                case var other: throw new JsonReaderException($"Invalid JSON data. Expected array or object. Found {other}");
            }
        }
    }
}