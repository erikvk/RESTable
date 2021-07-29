using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

        public async ValueTask<long> SerializeCollectionAsync<T>(Utf8JsonWriter writer, IAsyncEnumerable<T> collectionObject, CancellationToken cancellationToken) where T : class
        {
            writer.WriteStartArray();
            var count = 0L;
            await foreach (var item in collectionObject.WithCancellation(cancellationToken).ConfigureAwait(false))
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

#if NET6_0
        /// <inheritdoc />
        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream body, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<T>(body, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                yield return item!;
            }
        }
#else
        /// <inheritdoc />
        public async IAsyncEnumerable<T> DeserializeCollection<T>(Stream body, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            IEnumerable<T> ReadEnumeration()
            {
                using var reader = new Utf8JsonStreamReader(body);
                reader.Read();
                switch (reader.TokenType)
                {
                    case JsonTokenType.None: yield break;
                    case JsonTokenType.StartObject:
                    {
                        var value = reader.Deserialize<T>();
                        if (value is not null)
                            yield return value;
                        break;
                    }
                    case JsonTokenType.StartArray:
                    {
                        reader.Read();
                        while (reader.TokenType != JsonTokenType.EndArray)
                        {
                            var value = reader.Deserialize<T>();
                            if (value is not null)
                                yield return value;
                            reader.Read();
                        }
                        break;
                    }
                    case var other: throw new JsonException($"Invalid JSON data. Expected array or object. Found {other}");
                }
            }

            foreach (var item in ReadEnumeration())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
#endif
    }
}