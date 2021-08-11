﻿using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        ValueTask PopulateAsync<T>(T target, string json) where T : notnull;
        ValueTask PopulateAsync<T>(T target, JsonElement json) where T : notnull;

        PopulatorAction GetPopulator<T>(JsonElement json) where T : notnull;
        PopulatorAction GetPopulator<T>(string json) where T : notnull;
        PopulatorAction GetPopulator(Type toPopulate, JsonElement jsonElement);
        PopulatorAction GetPopulator(Type toPopulate, string json);

        string Serialize(object value, bool? prettyPrint = null, bool ignoreNulls = false);
        Task SerializeAsync<T>(Stream stream, T entity, bool? prettyPrint = null, bool ignoreNulls = false, CancellationToken cancellationToken = new());

        T? Deserialize<T>(Span<byte> span);
        T? Deserialize<T>(string json);
        object? Deserialize(Type targetType, Span<byte> span);

        ValueTask<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken);
        ValueTask<object?> DeserializeAsync(Stream stream, Type targetType, CancellationToken cancellationToken);

        object? ToObject(JsonElement element, Type targetType);
        T? ToObject<T>(JsonElement element);
        JsonElement ToJsonElement<T>(T obj);
        JsonElement ToJsonElement(object obj, Type targetType);
    }
}