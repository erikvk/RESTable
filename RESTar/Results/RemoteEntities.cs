using System.IO;

namespace RESTar.Results
{
    public class RemoteEntities : Content
    {
        public RemoteEntities(IRequest request, Stream stream) : base(request)
        {
            Body = stream;
        }
    }
}