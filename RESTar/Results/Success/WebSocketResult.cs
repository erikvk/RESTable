﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;
using RESTar.Results.Error;

namespace RESTar.Results.Success
{
    internal class SwitchedTerminal : WebSocketResult
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.OK;
        public override string StatusDescription => "Switched terminal";
        internal SwitchedTerminal(IRequest request) : base(request) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade request failed
    /// </summary>
    public class WebSocketUpgradeFailed : WebSocketResult
    {
        private RESTarError Error { get; }

        /// <inheritdoc />
        public override HttpStatusCode StatusCode => Error.StatusCode;

        /// <inheritdoc />
        public override string StatusDescription => Error.StatusDescription;

        /// <inheritdoc />
        internal WebSocketUpgradeFailed(RESTarError error, IRequest request) : base(request)
        {
            Headers = error.Headers;
            Error = error;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Returned when a WebSocket upgrade was performed successfully, and RESTar has taken over the 
    /// context from the network provider.
    /// </summary>
    public class WebSocketUpgradeSuccessful : WebSocketResult
    {
        /// <inheritdoc />
        public override HttpStatusCode StatusCode => HttpStatusCode.SwitchingProtocols;

        /// <inheritdoc />
        public override string StatusDescription => "Switching protocols";

        internal WebSocketUpgradeSuccessful(IRequest request) : base(request) { }
    }

    /// <inheritdoc />
    /// <summary>
    /// A base class for results generated by WebSocket requests
    /// </summary>
    public abstract class WebSocketResult : ISerializedResult
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Context Context { get; }

        /// <inheritdoc />
        public abstract HttpStatusCode StatusCode { get; }

        /// <inheritdoc />
        public abstract string StatusDescription { get; }

        /// <inheritdoc />
        public Stream Body => default;

        /// <inheritdoc />
        public ContentType? ContentType => default;

        /// <inheritdoc />
        public ICollection<string> Cookies => default;

        /// <inheritdoc />
        public Headers Headers { get; protected set; }

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
        public IEnumerable<T> ToEntities<T>() where T : class =>
            throw new InvalidCastException($"Cannot convert {nameof(WebSocketUpgradeSuccessful)} to Entities");

        /// <inheritdoc />
        public TimeSpan TimeElapsed { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public WebSocketResult(IRequest request)
        {
            TraceId = request.TraceId;
            Context = request.Context;
            LogTime = DateTime.Now;
            TimeElapsed = request.TimeElapsed;
        }
    }
}