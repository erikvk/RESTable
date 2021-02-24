using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders.NativeJsonProtocol;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, RESTableJsonWriter textWriter, CancellationToken cancellationToken) where T : class;
    }
}