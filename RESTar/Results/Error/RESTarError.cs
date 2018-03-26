using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Requests;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Success;
using RESTar.Starcounter;
using static RESTar.Starcounter.Transact;

namespace RESTar.Results.Error
{
    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class RESTarError : Exception, ITraceable, ISerializedResult
    {
        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; } = new Headers();

        /// <inheritdoc />
        public ICollection<string> Cookies { get; } = new List<string>();

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        internal void SetTrace(IRequest request)
        {
            TraceId = request.TraceId;
            Context = request.Context;
            Request = request;
        }

        /// <inheritdoc />
        public string TraceId { get; private set; }

        /// <inheritdoc />
        public Context Context { get; private set; }

        /// <inheritdoc />
        public LogEventType LogEventType => LogEventType.HttpOutput;

        /// <inheritdoc />
        public string LogMessage
        {
            get
            {
                var info = Headers["RESTar-Info"];
                var errorInfo = Headers["ErrorInfo"];
                var tail = "";
                if (info != null)
                    tail += $". {info}";
                if (errorInfo != null)
                    tail += $" (see {errorInfo})";
                return $"{StatusCode.ToCode()}: {StatusDescription}{tail}";
            }
        }

        /// <inheritdoc />
        public string LogContent { get; } = null;

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        internal RESTarError(ErrorCodes code, string message) : base(message)
        {
            ExcludeHeaders = false;
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal RESTarError(ErrorCodes code, string message, Exception ie) : base(message, ie)
        {
            ExcludeHeaders = false;
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        /// <inheritdoc />
        public Stream Body { get; } = null;

        /// <inheritdoc />
        public ContentType? ContentType { get; } = null;

        /// <inheritdoc />
        public ISerializedResult Serialize(ContentType? contentType = null) => this;

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>() where T : class => throw this;

        /// <inheritdoc />
        public void ThrowIfError() => throw this;

        internal static RESTarError GetError(Exception exception)
        {
            switch (exception)
            {
                case RESTarError re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException _: return new FailedJsonDeserialization(exception);
                case global::Starcounter.DbException _ when exception.Message.Contains("SCERR4034"): return new AbortedByCommitHook(exception);
                case global::Starcounter.DbException _: return new StarcounterDatabaseError(exception);
                case RuntimeBinderException _: return new BinderPermissions(exception);
                case NotImplementedException _: return new FeatureNotImplemented("RESTar encountered a call to a non-implemented method");
                default: return new Unknown(exception);
            }
        }

        internal static ISerializedResult GetResult(Exception exs, IRequestInternal request)
        {
            var error = GetError(exs);
            error.SetTrace(request);
            string errorId = null;
            if (!(error is Forbidden.Forbidden))
            {
                Admin.Error.ClearOld();
                errorId = Trans(() => Admin.Error.Create(error, request)).Id;
            }
            if (request.IsWebSocketUpgrade)
            {
                request.Context.WebSocket?.SendResult(error);
                request.Context.WebSocket?.Disconnect();
                return new WebSocketUpgradeSuccessful(request);
            }
            if (errorId != null)
                error.Headers["ErrorInfo"] = $"/{typeof(Admin.Error).FullName}/id={HttpUtility.UrlEncode(errorId)}";
            error.TimeElapsed = request.TimeElapsed;
            return error;
        }

        /// <summary>
        /// The request that generated the error
        /// </summary>
        public IRequest Request { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// The time elapsed from the start of reqeust evaluation
        /// </summary>
        public TimeSpan TimeElapsed { get; private set; }

        /// <inheritdoc />
        public override string ToString() => LogMessage;
    }
}