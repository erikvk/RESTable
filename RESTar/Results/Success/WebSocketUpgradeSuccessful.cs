using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar.Results.Success
{
    internal class WebSocketUpgradeSuccessful : IFinalizedResult
    {
        HttpStatusCode IResult.StatusCode => default;
        string IResult.StatusDescription => default;
        Stream IFinalizedResult.Body => default;
        public ContentType ContentType => default;
        ICollection<string> IResult.Cookies => default;
        Headers ILogable.Headers => default;
        public string TraceId { get; }
        public LogEventType LogEventType => default;
        public string LogMessage => default;
        public string LogContent => default;
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders => default;
        public IFinalizedResult FinalizeResult(ContentType? contentType = null) => this;
        public void ThrowIfError() { }
        public IEnumerable<T> ToEntities<T>() => throw new InvalidCastException($"Cannot convert {nameof(WebSocketUpgradeSuccessful)} to Entities");
        public Context Context { get; }
        public TimeSpan TimeElapsed { get; }
        public DateTime LogTime { get; }

        public WebSocketUpgradeSuccessful(ITraceable trace)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            LogTime = DateTime.Now;
            TimeElapsed = default;
        }
    }
}