using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Meta;

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
        private TypeCache TypeCache { get; }

        private ArrayPool<byte> BufferPool { get; }

        private const string JsonMimeType = "application/json";
        private const string RESTableSpecific = "application/restable-json";
        private const string Brief = "json";
        private const string TextPlain = "text/plain";

        public string Name => "JSON";
        public ContentType ContentType => "application/json; charset=utf-8";
        public bool CanRead => true;
        public bool CanWrite => true;
        public string ContentDispositionFileExtension => ".json";
        public string[] MatchStrings => new[] { JsonMimeType, RESTableSpecific, Brief, TextPlain };

        /// <summary>
        /// Creates a new instance of the <see cref="SystemTextJsonProvider"/> type
        /// </summary>
        public SystemTextJsonProvider(JsonSerializerOptionsAccessor optionsAccessor, ConverterResolver resolver, TypeCache typeCache)
        {
            Options = new JsonSerializerOptions(optionsAccessor.Options);
            Options.Converters.Add(resolver);
            OptionsIgnoreNulls = new JsonSerializerOptions(Options) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
            BufferPool = ArrayPool<byte>.Create(Options.DefaultBufferSize, 50);
            TypeCache = typeCache;
        }

        private JsonSerializerOptions GetOptions(bool? prettyPrint, bool ignoreNulls)
        {
            var options = ignoreNulls ? OptionsIgnoreNulls : Options;
            if (prettyPrint.HasValue && prettyPrint.Value != options.WriteIndented)
                options = new JsonSerializerOptions(options) { WriteIndented = prettyPrint.Value };
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
        public Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken = new())
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
        public async ValueTask<long> SerializeCollectionAsync<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken)
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

        private PopulateSource GetPopulateSource(JsonElement jsonElement)
        {
            var valueResolver = new JsonElementValueProvider(jsonElement, this);
            SourceKind sourceKind;
            (string, PopulateSource)[]? properties = null;
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    sourceKind = SourceKind.Object;
                    properties = jsonElement.EnumerateObject()
                        .Select(property => (property.Name, GetPopulateSource(property.Value)))
                        .ToArray();
                    break;
                case JsonValueKind.Array:
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    sourceKind = SourceKind.Value;
                    break;
                case JsonValueKind.Null:
                    sourceKind = SourceKind.Null;
                    break;
                case var other: throw new InvalidOperationException($"Cannot populate from JSON token with value kind '{other}'");
            }
            return new PopulateSource(sourceKind, valueResolver, properties);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken) where T : notnull
        {
#if NETSTANDARD2_0
            using (stream)
#else
            await using (stream.ConfigureAwait(false))
#endif
            {
                var jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(stream, Options, cancellationToken).ConfigureAwait(false);
                var populateSource = GetPopulateSource(jsonElement);
                var populator = new Populator(typeof(T), populateSource, TypeCache);
                await foreach (var item in entities.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return (T) await populator.PopulateAsync(item).ConfigureAwait(false);
                }
            }
        }

        public PopulatorAction GetPopulator(Type toPopulate, JsonElement jsonElement)
        {
            var populateSource = GetPopulateSource(jsonElement);
            return new Populator(toPopulate, populateSource, TypeCache).PopulateAsync;
        }

        public PopulatorAction GetPopulator(Type toPopulate, string json)
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, Options);
            var populateSource = GetPopulateSource(jsonElement);
            return new Populator(toPopulate, populateSource, TypeCache).PopulateAsync;
        }

        public PopulatorAction GetPopulator<T>(JsonElement jsonElement) where T : notnull
        {
            var populateSource = GetPopulateSource(jsonElement);
            return new Populator(typeof(T), populateSource, TypeCache).PopulateAsync;
        }

        public PopulatorAction GetPopulator<T>(string json) where T : notnull
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, Options);
            var populateSource = GetPopulateSource(jsonElement);
            return new Populator(typeof(T), populateSource, TypeCache).PopulateAsync;
        }

        public async ValueTask PopulateAsync<T>(T target, string json, CancellationToken cancellationToken) where T : notnull
        {
            var populator = GetPopulator<T>(json);
            await populator(target).ConfigureAwait(false);
        }

        public async ValueTask PopulateAsync<T>(T target, JsonElement json, CancellationToken cancellationToken) where T : notnull
        {
            var populator = GetPopulator<T>(json);
            await populator(target).ConfigureAwait(false);
        }

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
        public object? ToObject(JsonElement element, Type targetType) => element.ToObject(targetType, Options);
        public JsonElement ToJsonElement(object obj, Type targetType) => obj.ToJsonElement(targetType, Options);

        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream stream, [EnumeratorCancellation] CancellationToken cancellationToken)
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
                    if (next is not null)
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
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        // Empty array
                        hasValue = false;
                        next = default;
                        break;
                    }
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