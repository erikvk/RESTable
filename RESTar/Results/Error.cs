using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Requests;
using RESTar.Starcounter;
using Starcounter;

namespace RESTar.Results
{
    public abstract class RequestError : Error
    {
        /// <summary>
        /// The request that generated this result
        /// </summary>
        public IRequest Request { get; }

        private IRequestInternal RequestInternal { get; }

        private Stream _body;
        private bool IsSerializing;

        /// <inheritdoc />
        public override Stream Body => _body ?? (IsSerializing ? _body = new RESTarOutputStreamController() : null);

        /// <inheritdoc />
        public override ISerializedResult Serialize(ContentType? contentType = null)
        {
            IsSerializing = true;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider;
                var acceptProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, contentType);
                ContentType = acceptProvider.ContentType;
                var serialized = protocolProvider.ProtocolProvider.Serialize(this, acceptProvider);
                if (serialized is RequestError rr && rr._body is RESTarOutputStreamController rsc)
                    _body = rsc.Stream;
                return serialized;
            }
            catch (Exception exception)
            {
                return Error.GetResult(exception, RequestInternal).Serialize();
            }
            finally
            {
                IsSerializing = false;
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
                Headers["RESTar-elapsed-ms"] = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <inheritdoc />
        protected RequestError(IRequest request, ErrorCodes code, string info, Exception ie = null) : base(request, code, info, ie)
        {
            Request = request;
            RequestInternal = (IRequestInternal) request;
            TimeElapsed = request.TimeElapsed;
        }

        private CachedProtocolProvider CachedProtocolProvider { get; set; }
        private IContentTypeProvider ContentTypeProvider { get; set; }

        internal static IResult GetResult(Exception exs, IRequestInternal request)
        {
            var error = GetError(exs);
            error.CachedProtocolProvider = request.CachedProtocolProvider;
            error.ContentTypeProvider = ContentTypeController.ResolveOutputContentTypeProvider(request, null);
            if (request.IsWebSocketUpgrade)
            {
                if (error is Forbidden)
                {
                    request.Context.WebSocket.Disconnect();
                    return new WebSocketUpgradeFailed(error);
                }
                request.Context.WebSocket?.SendResult(error);
                request.Context.WebSocket?.Disconnect();
                return new WebSocketUpgradeSuccessful(request);
            }
            return error;
        }

        internal void Store()
        {
            if (this is Forbidden) return;
            string errorId = null;
            Admin.Error.ClearOld();
            Db.TransactAsync(() => errorId = Admin.Error.Create(this, Request).Id);
            if (errorId != null)
                Headers["RESTar-debug"] = $"/restar.admin.error/id={HttpUtility.UrlEncode(errorId)}";
        }
    }

    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class Error : Exception, ITraceable, ISerializedResult
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Context Context { get; }

        /// <summary>
        /// The error code for this error
        /// </summary>
        public ErrorCodes ErrorCode { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <inheritdoc />
        public ICollection<string> Cookies { get; }

        #region ITraceable, ILogable

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
        public string LogContent { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        #endregion

        internal Error(ITraceable trace, ErrorCodes code, string info, Exception ie = null) : base(info, ie)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            ExcludeHeaders = false;
            Cookies = new List<string>();
            LogContent = null;
            LogTime = DateTime.Now;
            Headers = new Headers();
            Body = null;
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        /// <inheritdoc />
        public virtual Stream Body { get; }

        /// <inheritdoc />
        public ContentType? ContentType { get; protected set; }

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>() where T : class => throw this;

        /// <inheritdoc />
        public void ThrowIfError() => throw this;

        internal static Error GetError(Exception exception)
        {
            switch (exception)
            {
                case Error re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException _: return new FailedJsonDeserialization(exception);
                case DbException _ when exception.Message.Contains("SCERR4034"): return new AbortedByCommitHook(exception);
                case DbException _: return new StarcounterDatabaseError(exception);
                case RuntimeBinderException _: return new BinderPermissions(exception);
                case NotImplementedException _: return new FeatureNotImplemented("RESTar encountered a call to a non-implemented method");
                default: return new Unknown(exception);
            }
        }

        /// <inheritdoc />
        public virtual ISerializedResult Serialize(ContentType? contentType = null) => this;

        /// <inheritdoc />
        /// <summary>
        /// The time elapsed from the start of reqeust evaluation
        /// </summary>
        public TimeSpan TimeElapsed { get; protected set; }

        /// <inheritdoc />
        public override string ToString() => LogMessage;
    }
}