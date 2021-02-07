using System.IO;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Selects a stream and content type for a binary resource
    /// </summary>
    internal delegate Task<(Stream stream, ContentType contentType)> BinarySelector<T>(IRequest<T> request) where T : class;
}