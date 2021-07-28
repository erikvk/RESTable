using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

// Async disposal differs between target frameworks
#pragma warning disable 1998

namespace RESTable.Json
{
    /// <inheritdoc cref="RESTable.ContentTypeProviders.IJsonProvider" />
    /// <inheritdoc cref="RESTable.ContentTypeProviders.IContentTypeProvider" />
    public class SystemTextJsonProvider : IJsonProvider, IContentTypeProvider
    {
        private JsonSerializerOptions Options { get; }
        private JsonSerializerOptions OptionsIgnoreNulls { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTableSpecific = "application/restable-json";
        private const string Brief = "json";
        private const string TextPlain = "text/plain";

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
        public string[] MatchStrings { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="SystemTextJsonProvider"/> type
        /// </summary>
        public SystemTextJsonProvider(JsonSerializerOptions options)
        {
            Options = options;
            OptionsIgnoreNulls = new JsonSerializerOptions(options)
            {
                IgnoreNullValues = true
            };
            MatchStrings = new[] {JsonMimeType, RESTableSpecific, Brief, TextPlain};
            ContentDispositionFileExtension = ".json";
            CanWrite = true;
            CanRead = true;
            ContentType = "application/json; charset=utf-8";
            Name = "JSON";
        }


        private JsonSerializerOptions GetOptions(bool? prettyPrint, bool ignoreNulls)
        {
            var options = ignoreNulls ? OptionsIgnoreNulls : Options;
            if (prettyPrint.HasValue && prettyPrint.Value != options.WriteIndented)
                options = new JsonSerializerOptions(options) {WriteIndented = prettyPrint.Value};
            return options;
        }

        public string Serialize(object value, bool? prettyPrint = null, bool ignoreNulls = false)
        {
            var options = GetOptions(prettyPrint, ignoreNulls);
            return JsonSerializer.Serialize(value, options);
        }

        public Task SerializeAsync(IJsonWriter jsonWriter, object value) { }

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <returns></returns>
        public async Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken = new()) where T : class
        {
            await JsonSerializer.SerializeAsync(stream, item, Options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <returns></returns>
        public async Task SerializeAsync<T>(Stream stream, T entity, bool? prettyPrint, bool ignoreNulls = false, CancellationToken cancellationToken = new())
        {
            var options = GetOptions(prettyPrint, ignoreNulls);
            await JsonSerializer.SerializeAsync(stream, entity, options, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async ValueTask<long> SerializeCollectionAsync<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            var swr = new StreamWriter
            (
                stream: stream,
                encoding: Options.JsonSettings.Encoding,
                bufferSize: 4096,
                leaveOpen: true
            );
#if NETSTANDARD2_0
            using (swr)
#else
            await using (swr.ConfigureAwait(false))
#endif
            {
                using var jwr = new NewtonsoftJsonWriter(swr, Options.JsonSettings.LineEndings, 0)
                {
                    Formatting = Options.JsonSettings.PrettyPrint ? Indented : None
                };
                jwr.StartCountObjectsWritten();
                Serializer.Serialize(jwr, collection.ToEnumerable());
                return jwr.StopCountObjectsWritten();
            }
        }

        /// <inheritdoc />
        public ValueTask<long> SerializeCollectionAsync<T>(IJsonWriter textWriter, IAsyncEnumerable<T> collectionObject, CancellationToken cancellationToken)
            where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();
            textWriter.StartCountObjectsWritten();
            Serializer.Serialize((NewtonsoftJsonWriter) textWriter, collectionObject.ToEnumerable());
            var objectsWritten = textWriter.StopCountObjectsWritten();
            return Task.FromResult(objectsWritten);
        }

        public IJsonWriter GetJsonWriter(TextWriter writer)
        {
            return new NewtonsoftJsonWriter(writer, Options.JsonSettings.LineEndings, 0)
            {
                Formatting = Options.JsonSettings.PrettyPrint ? Indented : None
            };
        }


        /// <summary>
        /// Populates JSON data onto an object
        /// </summary>
        public void Populate(object target, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            JsonConvert.PopulateObject(json, target, Options.SerializerSettings);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body)
        {
            var json = Encoding.UTF8.GetString(body);
            await foreach (var entity in entities.ConfigureAwait(false))
            {
                JsonConvert.PopulateObject(json, entity!, Options.SerializerSettings);
                yield return entity;
            }
        }

        public void Populate(object target, JsonElement json)
        {
            throw new NotImplementedException();
        }

        public T? Deserialize<T>(byte[] bytes)
        {
            return JsonSerializer.Deserialize<T>(bytes, Options);
        }

        public T? Deserialize<T>(byte[] bytes, int offset, int count)
        {
            var span = new ReadOnlySpan<byte>(bytes, offset, count);
            return JsonSerializer.Deserialize<T>(span, Options);
        }

        public T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public object? Deserialize(Type targetType, byte[] bytes)
        {
            return JsonSerializer.Deserialize(bytes, targetType, Options);
        }

        public object? Deserialize(Type targetType, byte[] bytes, int offset, int count)
        {
            var span = new ReadOnlySpan<byte>(bytes, offset, count);
            return JsonSerializer.Deserialize(span, targetType, Options);
        }

        public ValueTask<T?> DeserializeAsync<T>(Stream stream)
        {
            return JsonSerializer.DeserializeAsync<T>(stream, Options);
        }

        public ValueTask<object?> DeserializeAsync(Stream stream, Type targetType)
        {
            return JsonSerializer.DeserializeAsync(stream, targetType, Options);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream body)
        {
            using var streamReader = new StreamReader
            (
                stream: body,
                encoding: Options.JsonSettings.Encoding,
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
                {
                    var value = Serializer.Deserialize<T>(jsonReader);
                    if (value is not null)
                        yield return value;
                    break;
                }
                case JsonToken.StartArray:
                {
                    await jsonReader.ReadAsync().ConfigureAwait(false);
                    while (jsonReader.TokenType != JsonToken.EndArray)
                    {
                        var value = Serializer.Deserialize<T>(jsonReader);
                        if (value is not null)
                            yield return value;
                        await jsonReader.ReadAsync().ConfigureAwait(false);
                    }
                    break;
                }
                case var other: throw new JsonReaderException($"Invalid JSON data. Expected array or object. Found {other}");
            }
        }
    }
}