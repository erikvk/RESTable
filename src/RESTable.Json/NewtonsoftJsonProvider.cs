﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RESTable.ContentTypeProviders;
using RESTable.Meta;
using RESTable.Requests;
using static Newtonsoft.Json.Formatting;

namespace RESTable.Json
{
    /// <inheritdoc cref="RESTable.ContentTypeProviders.IJsonProvider" />
    /// <inheritdoc cref="RESTable.ContentTypeProviders.IContentTypeProvider" />
    public class NewtonsoftJsonProvider : IJsonProvider, IContentTypeProvider
    {
        /// <summary>
        /// The settings that are used when serializing and deserializing JSON
        /// </summary>
        private JsonSerializerSettings SerializerSettings { get; }

        private JsonSettings JsonSettings { get; }

        private JsonSerializerSettings SerialzerSettingsIgnoreNulls { get; }

        /// <summary>
        /// The JSON serializer
        /// </summary>
        internal JsonSerializer Serializer { get; }

        /// <summary>
        /// Creates a new JSON.net JsonSerializer with the current RESTable serialization settings
        /// </summary>
        /// <returns></returns>
        public JsonSerializer GetSerializer() => JsonSerializer.Create(SerializerSettings);

        private JsonSerializer SerializerIgnoreNulls { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTableSpecific = "application/restable-json";
        private const string Brief = "json";
        private const string TextPlain = "text/plain";

        /// <summary>
        /// Creates a new instance of the <see cref="NewtonsoftJsonProvider"/> type
        /// </summary>
        public NewtonsoftJsonProvider(TypeCache typeCache, JsonSettings jsonSettings)
        {
            JsonSettings = jsonSettings;
            SerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include,
                DateParseHandling = DateParseHandling.DateTime,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DefaultResolver(typeCache),
                FloatParseHandling = FloatParseHandling.Decimal
            };
            Serializer = JsonSerializer.Create(SerializerSettings);
            SerialzerSettingsIgnoreNulls = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = SerializerSettings.DateParseHandling,
                DateFormatHandling = SerializerSettings.DateFormatHandling,
                DateTimeZoneHandling = SerializerSettings.DateTimeZoneHandling,
                ContractResolver = SerializerSettings.ContractResolver,
                FloatParseHandling = SerializerSettings.FloatParseHandling
            };
            SerializerIgnoreNulls = JsonSerializer.Create(SerialzerSettingsIgnoreNulls);
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

        JsonSerializer IJsonProvider.GetSerializer()
        {
            return GetSerializer();
        }

        /// <summary>
        /// A general-purpose serializer. Serializes the given value using the formatting, and optionally - a type. If no 
        /// formatting is given, the formatting defined in Settings is used.
        /// </summary>
        public string Serialize(object value, bool? prettyPrint, bool ignoreNulls = false)
        {
            var formatting = prettyPrint ?? JsonSettings.PrettyPrint ? Indented : None;
            var settings = ignoreNulls ? SerialzerSettingsIgnoreNulls : SerializerSettings;
            return JsonConvert.SerializeObject(value, formatting, settings);
        }

        /// <summary>
        /// A general-purpose deserializer. Deserializes the given JSON string.
        /// </summary>
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }

        /// <summary>
        /// A general-purpose deserializer. Deserializes the given byte array.
        /// </summary>
        public T Deserialize<T>(byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }

        /// <summary>
        /// Serializes the value to the given JsonTextWriter, using the default serializer
        /// </summary>
        public void Serialize(IJsonWriter jsonWriter, object value)
        {
            Serializer.Serialize((JsonTextWriter) jsonWriter, value);
        }

        /// <summary>
        /// Populates JSON data onto an object
        /// </summary>
        public void Populate(string json, object target)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JsonConvert.PopulateObject(json, target, SerializerSettings);
        }

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <returns></returns>
        public void SerializeToStream(Stream stream, object entity, bool? prettyPrint, bool ignoreNulls = false)
        {
            var formatting = prettyPrint ?? JsonSettings.PrettyPrint ? Indented : None;
            var serializer = ignoreNulls ? SerializerIgnoreNulls : Serializer;
            using var swr = new StreamWriter(stream, JsonSettings.Encoding, 1024, true);
            using var jwr = new NewtonsoftJsonWriter(swr, JsonSettings.LineEndings, 0) {Formatting = formatting};
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
                JsonConvert.PopulateObject(json, entity, SerializerSettings);
                yield return entity;
            }
        }

        /// <inheritdoc />
        public string[] MatchStrings { get; set; }

        /// <inheritdoc />
        public async Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collection, Stream stream, IRequest request, CancellationToken cancellationToken) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (collection == null) return 0;
            await using var swr = new StreamWriter
            (
                stream: stream,
                encoding: JsonSettings.Encoding,
                bufferSize: 2048,
                leaveOpen: true
            );
            using var jwr = new NewtonsoftJsonWriter(swr, JsonSettings.LineEndings, 0)
            {
                Formatting = JsonSettings.PrettyPrint ? Indented : None
            };
            Serializer.Serialize(jwr, collection.ToEnumerable());
            return jwr.ObjectsWritten;
        }

        /// <inheritdoc />
        public Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, IJsonWriter textWriter, CancellationToken cancellationToken)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (collectionObject == null) return Task.FromResult<long>(0);
            var preWritten = textWriter.ObjectsWritten;
            Serializer.Serialize((NewtonsoftJsonWriter) textWriter, collectionObject.ToEnumerable());
            return Task.FromResult(textWriter.ObjectsWritten - preWritten);
        }

        public IJsonWriter GetJsonWriter(TextWriter writer)
        {
            return new NewtonsoftJsonWriter(writer, JsonSettings.LineEndings, 0) {Formatting = JsonSettings.PrettyPrint ? Indented : None};
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream body)
        {
            using var streamReader = new StreamReader
            (
                stream: body,
                encoding: JsonSettings.Encoding,
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