using System.IO;
using System.Net;
using RESTar.Requests;

namespace RESTar.Operations
{
    public abstract class Result : IFinalizedResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public MemoryStream Body { get; set; }
        Stream IFinalizedResult.Body => Body;
        public string ContentType { get; set; }
        public Headers Headers { get; }
        public bool HasContent => Body?.Length > 0;

        internal Result() => Headers = new Headers();
    }
}