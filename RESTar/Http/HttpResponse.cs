using System.IO;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;
using Starcounter;
using static System.StringSplitOptions;

namespace RESTar.Http
{
    internal class HttpResponse : IFinalizedResult
    {
        public HttpStatusCode StatusCode { get; }
        public string StatusDescription { get; }
        public long ContentLength { get; }
        public string ContentType { get; }
        public Stream Body { get; }
        public Headers Headers { get; }
        public ICollection<string> Cookies { get; }
        internal bool IsSuccessStatusCode => StatusCode >= (HttpStatusCode) 200 && StatusCode < (HttpStatusCode) 300;
        public string TraceId { get; }
        public TCPConnection TcpConnection { get; }

        public LogEventType LogEventType { get; } = LogEventType.HttpOutput;
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription}";
        public string LogContent { get; } = null;
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders { get; }

        private HttpResponse(ITraceable trace)
        {
            ExcludeHeaders = false;
            TraceId = trace.TraceId;
            TcpConnection = trace.TcpConnection;
        }

        internal HttpResponse(ITraceable trace, HttpStatusCode statusCode, string statusDescription) : this(trace)
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        internal HttpResponse(ITraceable trace, HttpWebResponse webResponse) : this(trace)
        {
            StatusCode = webResponse.StatusCode;
            StatusDescription = webResponse.StatusDescription;
            ContentLength = webResponse.ContentLength;
            ContentType = webResponse.ContentType;
            Body = webResponse.GetResponseStream() ?? throw new NullReferenceException("ResponseStream was null");
            Headers = new Headers();
            Cookies = new List<string>();
            foreach (var header in webResponse.Headers.AllKeys)
                Headers[header] = webResponse.Headers[header];
        }

        internal HttpResponse(ITraceable trace, Response scResponse) : this(trace)
        {
            StatusCode = (HttpStatusCode) scResponse.StatusCode;
            StatusDescription = scResponse.StatusDescription;
            ContentLength = scResponse.ContentLength;
            ContentType = scResponse.ContentType;
            Body = scResponse.StreamedBody;
            Headers = new Headers();
            Cookies = new List<string>();
            scResponse.GetAllHeaders()
                .Split("\r\n", RemoveEmptyEntries)
                .Select(s => s.TSplit(':'))
                .ForEach(tuple =>
                {
                    var (name, value) = tuple;
                    if (name.EqualsNoCase("Set-Cookie"))
                        Cookies.Add(value.Trim());
                    else Headers[name] = value.Trim();
                });
        }
    }
}