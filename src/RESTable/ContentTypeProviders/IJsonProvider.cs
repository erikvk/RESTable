using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.ContentTypeProviders
{
    public interface IJsonProvider : IContentTypeProvider
    {
        Task<long> SerializeCollection<T>(IAsyncEnumerable<T> collectionObject, Stream stream, int baseIndentation, IRequest request = null) where T : class;
    }
}