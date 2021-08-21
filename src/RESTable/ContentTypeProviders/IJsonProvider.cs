using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        string Serialize(object value, bool? prettyPrint = null, bool ignoreNulls = false);
        string Serialize<T>(T value, bool? prettyPrint = null, bool ignoreNulls = false);
        string Serialize(object value, Type inputType, bool? prettyPrint = null, bool ignoreNulls = false);
        byte[] SerializeToUtf8Bytes(object value, bool? prettyPrint = null, bool ignoreNulls = false);
        byte[] SerializeToUtf8Bytes(object value, Type inputType, bool? prettyPrint = null, bool ignoreNulls = false);
        byte[] SerializeToUtf8Bytes<T>(T value, bool? prettyPrint = null, bool ignoreNulls = false);
        Task SerializeAsync<T>(Stream stream, T entity, bool? prettyPrint = null, bool ignoreNulls = false, CancellationToken cancellationToken = new());

        T? Deserialize<T>(Span<byte> span, JsonSerializerOptions? options = null);
        T? Deserialize<T>(string json, JsonSerializerOptions? options = null);
        object? Deserialize(Type targetType, Span<byte> span, JsonSerializerOptions? options = null);

        JsonReader GetJsonReader(JsonSerializerOptions? options = null);
        JsonWriter GetJsonWriter(Utf8JsonWriter jsonWriter, JsonSerializerOptions? options = null);

        ValueTask<T?> DeserializeAsync<T>(Stream stream, JsonSerializerOptions? options = null, CancellationToken cancellationToken = new());
        ValueTask<object?> DeserializeAsync(Stream stream, Type targetType, JsonSerializerOptions? options = null, CancellationToken cancellationToken = new());

        ValueTask PopulateAsync<T>(T target, string json, JsonSerializerOptions? options = null) where T : notnull;
        ValueTask PopulateAsync<T>(T target, JsonElement json, JsonSerializerOptions? options = null) where T : notnull;
        ValueTask PopulateAsync(object target, Type targetType, string json, JsonSerializerOptions? options = null);
        ValueTask PopulateAsync(object target, Type targetType, JsonElement json, JsonSerializerOptions? options = null);

        void Populate<T>(T target, string json, JsonSerializerOptions? options = null) where T : notnull;
        void Populate<T>(T target, JsonElement json, JsonSerializerOptions? options = null) where T : notnull;
        void Populate(object target, Type targetType, string json, JsonSerializerOptions? options = null);
        void Populate(object target, Type targetType, JsonElement json, JsonSerializerOptions? options = null);

        PopulatorAction GetPopulator<T>(JsonElement json, JsonSerializerOptions? options = null) where T : notnull;
        PopulatorAction GetPopulator<T>(string json, JsonSerializerOptions? options = null) where T : notnull;
        PopulatorAction GetPopulator(Type toPopulate, JsonElement jsonElement, JsonSerializerOptions? options = null);
        PopulatorAction GetPopulator(Type toPopulate, string json, JsonSerializerOptions? options = null);

        object? ToObject(JsonElement element, Type targetType, JsonSerializerOptions? options = null);
        T? ToObject<T>(JsonElement element, JsonSerializerOptions? options = null);

        JsonElement ToJsonElement<T>(T obj, JsonSerializerOptions? options = null);
        JsonElement ToJsonElement(object obj, Type targetType, JsonSerializerOptions? options = null);
    }
}