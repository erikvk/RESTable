using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;

namespace RESTar.Results
{
    internal class SwitchedTerminal : Success
    {
        internal SwitchedTerminal(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.OK;
            StatusDescription = "Switched terminal";
            TimeElapsed = request.TimeElapsed;
        }
    }

    internal class ShellNoContent : Success
    {
        internal ShellNoContent(ITraceable trace, TimeSpan elapsed) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No content";
            TimeElapsed = elapsed;
        }
    }

    internal class ShellNoQuery : Success
    {
        internal ShellNoQuery(ITraceable trace) : base(trace)
        {
            StatusCode = HttpStatusCode.NoContent;
            StatusDescription = "No query";
            TimeElapsed = default;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade request failed
    /// </summary>
    public class WebSocketUpgradeFailed : SerializedResultWrapper
    {
        internal WebSocketUpgradeFailed(Error error) : base(error) { }
    }
    
    /// <inheritdoc />
    /// <summary>
    /// Wraps a result and maps operations to its members
    /// </summary>
    public abstract class SerializedResultWrapper : ISerializedResult
    {
        /// <inheritdoc />
        public string TraceId => Result.TraceId;

        /// <inheritdoc />
        public Context Context => Result.Context;

        /// <inheritdoc />
        public LogEventType LogEventType => Result.LogEventType;

        /// <inheritdoc />
        public string LogMessage => Result.LogMessage;

        /// <inheritdoc />
        public string LogContent => Result.LogContent;

        /// <inheritdoc />
        public Headers Headers => Result.Headers;

        /// <inheritdoc />
        public string HeadersStringCache
        {
            get => Result.HeadersStringCache;
            set => Result.HeadersStringCache = value;
        }

        /// <inheritdoc />
        public bool ExcludeHeaders => Result.ExcludeHeaders;

        /// <inheritdoc />
        public DateTime LogTime => Result.LogTime;

        /// <inheritdoc />
        public HttpStatusCode StatusCode => Result.StatusCode;

        /// <inheritdoc />
        public string StatusDescription => Result.StatusDescription;

        /// <inheritdoc />
        public ICollection<string> Cookies => Result.Cookies;

        /// <inheritdoc />
        public bool IsSerialized => Result.IsSerialized;

        /// <inheritdoc />
        public ISerializedResult Serialize(ContentType? contentType = null) => Result.Serialize();

        /// <inheritdoc />
        public void ThrowIfError() => Result.ThrowIfError();

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>() where T : class => Result.ToEntities<T>();

        /// <inheritdoc />
        public Stream Body
        {
            get => Result.Body;
            set => Result.Body = value;
        }

        /// <inheritdoc />
        public TimeSpan TimeElapsed => Result.TimeElapsed;

        /// <inheritdoc />
        public string Metadata => Result.Metadata;

        private readonly ISerializedResult Result;

        /// <inheritdoc />
        protected SerializedResultWrapper(ISerializedResult result) => Result = result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully, and RESTar has taken over the 
    /// context from the network provider.
    /// </summary>
    public class WebSocketUpgradeSuccessful : Success
    {
        internal WebSocketUpgradeSuccessful(IRequest request) : base(request)
        {
            StatusCode = HttpStatusCode.SwitchingProtocols;
            StatusDescription = "Switching protocols";
            TimeElapsed = request.TimeElapsed;
        }
    }
}