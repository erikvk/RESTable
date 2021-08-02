using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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

        private ArrayPool<byte> BufferPool { get; }

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
        public SystemTextJsonProvider(JsonSerializerOptionsAccessor optionsAccessor, ConverterResolver resolver)
        {
            Options = optionsAccessor.Options;
            Options.Converters.Add(resolver);
            OptionsIgnoreNulls = new JsonSerializerOptions(Options) {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull};
            BufferPool = ArrayPool<byte>.Create(Options.DefaultBufferSize, 50);
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

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <returns></returns>
        public Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken = new()) where T : class
        {
            return JsonSerializer.SerializeAsync(stream, item, Options, cancellationToken);
        }

        /// <summary>
        /// Serializes an object into a stream
        /// </summary>
        /// <returns></returns>
        public Task SerializeAsync<T>(Stream stream, T entity, bool? prettyPrint, bool ignoreNulls = false, CancellationToken cancellationToken = new())
        {
            var options = GetOptions(prettyPrint, ignoreNulls);
            return JsonSerializer.SerializeAsync(stream, entity, options, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask<long> SerializeCollectionAsync<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken) where T : class
        {
            await using var writer = new Utf8JsonWriter(stream);
            writer.WriteStartArray();
            var count = 0L;
            await foreach (var item in collection.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                JsonSerializer.Serialize(writer, item, Options);
                count += 1;
            }
            writer.WriteEndArray();
            return count;
        }

        /// <summary>
        /// Populates JSON data onto an object
        /// </summary>
        public void Populate(object target, string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, byte[] body)
        {
            yield break;
        }

        public void Populate(object target, JsonElement json) { }


        public T? Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public T? Deserialize<T>(Span<byte> span)
        {
            return JsonSerializer.Deserialize<T>(span, Options);
        }

        public object? Deserialize(Type targetType, Span<byte> span)
        {
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

        public T? ToObject<T>(JsonElement element) => element.ToObject<T>(Options);
        public JsonElement ToJsonElement<T>(T obj) => obj.ToJsonElement(Options);

        public async IAsyncEnumerable<T?> DeserializeCollection<T>(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var buffer = BufferPool.Rent(Options.DefaultBufferSize);
            JsonReaderState state = default;
            var leftOver = 0;
            try
            {
                while (true)
                {
#if NETSTANDARD2_0
                    var dataLength = await stream.ReadAsync(buffer, leftOver, buffer.Length - leftOver, cancellationToken).ConfigureAwait(false);
#else
                    var dataLength = await stream.ReadAsync(buffer.AsMemory(leftOver, buffer.Length - leftOver), cancellationToken).ConfigureAwait(false);
#endif
                    var dataSize = dataLength + leftOver;
                    var isFinalBlock = dataSize == 0;
                    if (isFinalBlock)
                        yield break;
                    var (bytesConsumed, next, hasValue) = GetNext<T>(buffer.AsSpan(0, dataSize), ref state);
                    if (!hasValue)
                        yield break;
                    yield return next;
                    leftOver = dataSize - (int) bytesConsumed;
                    if (leftOver != 0)
                        buffer.AsSpan(dataSize - leftOver, leftOver).CopyTo(buffer);
                }
            }
            finally
            {
                BufferPool.Return(buffer);
            }
        }

        private (long bytesConsumed, T? next, bool any) GetNext<T>(ReadOnlySpan<byte> dataUtf8, ref JsonReaderState state)
        {
            bool hasValue;
            T? next;
            var reader = new Utf8JsonReader(dataUtf8, false, state);
            reader.Read();
            switch (reader.TokenType)
            {
                case JsonTokenType.EndArray:
                case JsonTokenType.None:
                    next = default;
                    hasValue = false;
                    break;
                case JsonTokenType.StartObject:
                {
                    next = JsonSerializer.Deserialize<T>(ref reader, Options);
                    hasValue = true;
                    break;
                }
                case JsonTokenType.StartArray:
                {
                    reader.Read();
                    next = JsonSerializer.Deserialize<T>(ref reader, Options);
                    hasValue = true;
                    break;
                }
                case var other: throw new JsonException($"Invalid JSON data. Expected array or object. Found {other}");
            }
            state = reader.CurrentState;
            return (reader.BytesConsumed, next, hasValue);
        }
    }
}