using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal struct WebSocketResult : IFinalizedResult
    {
        internal bool LeaveOpen { get; }
        HttpStatusCode IResult.StatusCode => default;
        string IResult.StatusDescription => default;
        Stream IFinalizedResult.Body => default;
        public ContentType ContentType => default;
        ICollection<string> IResult.Cookies => default;
        Headers ILogable.Headers => default;
        public string TraceId { get; }
        public Client Client { get; }
        public LogEventType LogEventType => default;
        public string LogMessage => default;
        public string LogContent => default;
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders => default;
        public IFinalizedResult FinalizeResult(ContentType? contentType = null) => this;
        public void ThrowIfError() { }
        public IEnumerable<T> ToEntities<T>() => throw new InvalidCastException($"Cannot convert {nameof(WebSocketResult)} to Entities");

        public DateTime LogTime { get; }

        public WebSocketResult(bool leaveOpen, ITraceable trace) : this()
        {
            LeaveOpen = leaveOpen;
            TraceId = trace.TraceId;
            Client = trace.Client;
            LogTime = DateTime.Now;
        }
    }
}