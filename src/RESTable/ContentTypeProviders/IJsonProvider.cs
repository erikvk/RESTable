using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        void Populate<T>(T target, string json);
        void Populate<T>(T target, JsonElement json);

        string Serialize(object value, bool? prettyPrint = null, bool ignoreNulls = false);
        Task SerializeAsync<T>(Stream stream, T entity, bool? prettyPrint = null, bool ignoreNulls = false, CancellationToken cancellationToken = new());

        T? Deserialize<T>(Span<byte> span);
        T? Deserialize<T>(string json);
        object? Deserialize(Type targetType, Span<byte> span);

        ValueTask<T?> DeserializeAsync<T>(Stream stream);
        ValueTask<object?> DeserializeAsync(Stream stream, Type targetType);

        T? ToObject<T>(JsonElement element);
        JsonElement ToJsonElement<T>(T obj);
    }
}