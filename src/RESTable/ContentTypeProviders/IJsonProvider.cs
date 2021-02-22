using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RESTable.ContentTypeProviders.NativeJsonProtocol;
using RESTable.Requests;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, RESTableJsonWriter textWriter) where T : class;
    }
}