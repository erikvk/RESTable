using System.IO;
using System.Net;
using RESTable.Requests;

namespace RESTable.Internal
{
    internal class HttpResponse : ITraceable
    {
        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public long ContentLength { get; }
        public ContentType? ContentType { get; }
        public Stream? Body { get; }
        public Headers Headers { get; }
        internal bool IsSuccessStatusCode => StatusCode is >= (HttpStatusCode) 200 and < (HttpStatusCode) 300;
        public RESTableContext Context { get; }
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription} ({Body?.Length ?? 0} bytes) {Headers.Info}";

        internal HttpResponse(ITraceable trace, HttpStatusCode statusCode, string statusDescription)
        {
            Context = trace.Context;
            Headers = new Headers();
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        internal HttpResponse(ITraceable trace, HttpWebResponse webResponse)
        {
            Context = trace.Context;
            Headers = new Headers();
            StatusCode = webResponse.StatusCode;
            StatusDescription = webResponse.StatusDescription;
            ContentLength = webResponse.ContentLength;
            ContentType = webResponse.ContentType;
            Body = webResponse.GetResponseStream();
            foreach (var header in webResponse.Headers.AllKeys)
                Headers[header] = webResponse.Headers[header];
        }
    }
}