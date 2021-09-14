using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.Json
{
    public class JsonLinesProvider : IContentTypeProvider
    {
        public string Name => "JSON Lines";
        public ContentType ContentType => "application/jsonlines; charset=utf-8";
        public string[] MatchStrings => new[] {"application/jsonlines", "application/x-ndjson", "application/x-jsonlines", "jl"};
        public bool CanRead => false;
        public bool CanWrite => true;
        public string ContentDispositionFileExtension => ".jsonl";

        private static readonly byte[] NewLine = Encoding.UTF8.GetBytes("\n");

        private IJsonProvider JsonProvider { get; }

        public JsonLinesProvider(IJsonProvider jsonProvider)
        {
            JsonProvider = jsonProvider;
        }

        public byte[] SerializeToBytes<T>(T item)
        {
            return JsonProvider.SerializeToUtf8Bytes(item, prettyPrint: false);
        }

        public byte[] SerializeToBytes(object item, Type itemType)
        {
            return JsonProvider.SerializeToUtf8Bytes(item, itemType, prettyPrint: false);
        }

        public Task SerializeAsync<T>(Stream stream, T item, CancellationToken cancellationToken)
        {
            return JsonProvider.SerializeAsync(stream, item, prettyPrint: false, cancellationToken: cancellationToken);
        }

        public async ValueTask<long> SerializeAsyncEnumerable<T>(Stream stream, IAsyncEnumerable<T> collection, CancellationToken cancellationToken)
        {
            var count = 0L;
            await foreach (var item in collection.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (count > 0)
                {
#if !NETSTANDARD2_0
                    await stream.WriteAsync(NewLine, cancellationToken).ConfigureAwait(false);
#else
                    await stream.WriteAsync(NewLine, 0, NewLine.Length, cancellationToken).ConfigureAwait(false);
#endif
                }
                await JsonProvider.SerializeAsync(stream, item, prettyPrint: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                count += 1;
            }
            return count;
        }

        public IAsyncEnumerable<T> DeserializeAsyncEnumerable<T>(Stream stream, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<T> Populate<T>(IAsyncEnumerable<T> entities, Stream stream, CancellationToken cancellationToken) where T : notnull
        {
            throw new NotSupportedException();
        }
    }
}