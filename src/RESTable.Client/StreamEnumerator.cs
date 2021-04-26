using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders;

namespace RESTable.Client
{
    internal class StreamEnumerator<T> : IAsyncEnumerator<T> where T : class
    {
        private readonly IAsyncEnumerator<T> Enumerator;

        public StreamEnumerator(Stream stream, IContentTypeProvider contentTypeProvider, CancellationToken cancellationToken = new())
        {
            var enumerable = contentTypeProvider.DeserializeCollection<T>(stream);
            Enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await Enumerator.DisposeAsync();
        }

        public ValueTask<bool> MoveNextAsync() => Enumerator.MoveNextAsync();

        public T Current => Enumerator.Current;
    }
}