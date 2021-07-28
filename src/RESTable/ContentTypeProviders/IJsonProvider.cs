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
        void Populate(object target, string json);
        void Populate(object target, JsonElement json);

        IJsonWriter GetJsonWriter(TextWriter textWriter);

        Task SerializeAsync(IJsonWriter jsonWriter, object value);
        Task SerializeAsync<T>(Stream stream, T entity, bool? prettyPrint = null, bool ignoreNulls = false, CancellationToken cancellationToken = new());
        
        string Serialize(object value, bool? prettyPrint = null, bool ignoreNulls = false); 
        ValueTask<long> SerializeCollectionAsync<T>(IJsonWriter textWriter, IAsyncEnumerable<T> collectionObject, CancellationToken cancellationToken) where T : class;

        T? Deserialize<T>(byte[] bytes);
        T? Deserialize<T>(byte[] bytes, int offset, int count);
        T? Deserialize<T>(string json);
        object? Deserialize(Type targetType, byte[] bytes);
        object? Deserialize(Type targetType, byte[] bytes, int offset, int count);

        ValueTask<T?> DeserializeAsync<T>(Stream stream);
        ValueTask<object?> DeserializeAsync(Stream stream, Type targetType);
    }

    public interface IJsonWriter : IDisposable
    {
        void StartCountObjectsWritten();
        long StopCountObjectsWritten();
        Task WriteStartObjectAsync(CancellationToken cancellationToken);
        Task WritePropertyNameAsync(string status, CancellationToken cancellationToken);
        Task WriteEndObjectAsync(CancellationToken cancellationToken);
        Task WriteValueAsync(long value, CancellationToken cancellationToken);
        Task WriteValueAsync(double value, CancellationToken cancellationToken);
        Task WriteValueAsync(string value, CancellationToken cancellationToken);
        Task WriteValueAsync(bool value, CancellationToken cancellationToken);
    }
}