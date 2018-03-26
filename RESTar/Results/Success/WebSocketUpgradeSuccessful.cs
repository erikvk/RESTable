using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Queries;

namespace RESTar.Results.Success
{
    internal class SwitchedTerminal : OK
    {
        public SwitchedTerminal(ITraceable trace) : base(trace) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully
    /// </summary>
    public class WebSocketUpgradeSuccessful : ISerializedResult
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Context Context { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode => HttpStatusCode.SwitchingProtocols;

        /// <inheritdoc />
        public string StatusDescription => "Switching protocols";

        /// <inheritdoc />
        public Stream Body => default;

        /// <inheritdoc />
        public ContentType? ContentType => default;

        /// <inheritdoc />
        public ICollection<string> Cookies => default;

        /// <inheritdoc />
        public Headers Headers => default;

        /// <inheritdoc />
        public LogEventType LogEventType => LogEventType.HttpOutput;

        /// <inheritdoc />
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription}";

        /// <inheritdoc />
        public string LogContent => default;

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders => false;

        /// <inheritdoc />
        public ISerializedResult Serialize(ContentType? contentType = null) => this;

        /// <inheritdoc />
        public void ThrowIfError() { }

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>() where T : class => throw new InvalidCastException($"Cannot convert {nameof(WebSocketUpgradeSuccessful)} to Entities");

        /// <inheritdoc />
        public TimeSpan TimeElapsed { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public WebSocketUpgradeSuccessful(IQuery query)
        {
            TraceId = query.TraceId;
            Context = query.Context;
            LogTime = DateTime.Now;
            TimeElapsed = query.TimeElapsed;
        }
    }
}