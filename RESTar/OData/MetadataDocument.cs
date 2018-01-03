using System.IO;
using System.Net;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.OData
{
    internal class MetadataDocument : IFinalizedResult
    {
        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public Stream Body { get; }
        public string ContentType { get; }
        public Headers Headers { get; }
        public bool HasContent { get; }
    }
}
