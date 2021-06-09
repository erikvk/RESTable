﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, IJsonWriter textWriter, CancellationToken cancellationToken) where T : class;
        IJsonWriter GetJsonWriter(TextWriter writer);
        JsonSerializer GetSerializer();
        void Populate(string json, object target);
        void Serialize(IJsonWriter jsonWriter, object value);
        T? Deserialize<T>(byte[] bytes);
        T? Deserialize<T>(byte[] bytes, int offset, int count);
        T? Deserialize<T>(string json);
        object? Deserialize(Type targetType, byte[] bytes);
        object? Deserialize(Type targetType, byte[] bytes, int offset, int count);
        void SerializeToStream(Stream stream, object entity, bool? prettyPrint = null, bool ignoreNulls = false);
        string Serialize(object value, bool? prettyPrint = null, bool ignoreNulls = false);
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