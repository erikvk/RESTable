using System.IO;

namespace RESTar
{
    public interface IBucket<T> where T : class
    {
        (Stream stream, ContentType contentType) Select(IRequest<T> request);
    }
}