using System.Collections.Generic;
using System.IO;
using System.Net;

namespace RESTar.Operations
{
    internal interface IFinalizedResult
    {
        HttpStatusCode StatusCode { get; }
        string StatusDescription { get; }
        Stream Body { get; }
        string ContentType { get; }
        Dictionary<string, string> Headers { get; }
    }

    internal class Result : IFinalizedResult
    {
        internal IRequest Request { get; }
        internal string ExternalDestination { get; set; }
        internal IEnumerable<dynamic> Entities { get; set; }

        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public Stream Body { get; set; }
        public string ContentType { get; set; }
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        internal Result(IRequest request) => Request = request;
    }
}