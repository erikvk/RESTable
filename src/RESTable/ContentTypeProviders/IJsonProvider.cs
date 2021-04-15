using System;
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
        T Deserialize<T>(byte[] bytes);
        T Deserialize<T>(string json);
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
        Task WriteValueAsync(long invalidEntityIndex, CancellationToken cancellationToken);
        Task WriteValueAsync(double invalidEntityIndex, CancellationToken cancellationToken);
        Task WriteValueAsync(string fail, CancellationToken cancellationToken);
    }
}