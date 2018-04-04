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
    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class Error : Exception, ITraceable, ISerializedResult
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

        #region ITraceable, ILogable

        private void SetTrace(IRequest request)
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

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        #endregion

        internal Error(ErrorCodes code, string message) : base(message)
        {
            ExcludeHeaders = false;
            ErrorCode = code;
            Headers["RESTar-info"] = Message;
        }

        internal Error(ErrorCodes code, string message, Exception ie) : base(message, ie)
        {
            ExcludeHeaders = false;
            ErrorCode = code;
            if (message == null)
                Headers["RESTar-info"] = ie.Message;
            else Headers["RESTar-info"] = message;
        }

        /// <inheritdoc />
        public IEntities<T> ToEntities<T>() where T : class => throw this;

        /// <inheritdoc />
        public void ThrowIfError() => throw this;

        internal static Error GetError(Exception exception)
        {
            switch (exception)
            {
                case Error re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException jre: return new FailedJsonDeserialization(jre);
                case DbException _: return new StarcounterDatabaseError(exception);
                case RuntimeBinderException _: return new BinderPermissions(exception);
                case NotImplementedException _: return new FeatureNotImplemented("RESTar encountered a call to a non-implemented method");
                default: return new Unknown(exception);
            }
        }

        private IRequestInternal RequestInternal { get; set; }

        private Stream _body;
        private bool IsSerializing;

        /// <inheritdoc />
        public Stream Body => _body ?? (IsSerializing ? _body = new RESTarOutputStreamController() : null);

        private Stream GetStream() => Body;

        /// <inheritdoc />
        public bool IsSerialized { get; private set; }

        /// <inheritdoc />
        public ISerializedResult Serialize(ContentType? contentType = null)
        {
            IsSerializing = true;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var protocolProvider = RequestInternal.CachedProtocolProvider ?? ProtocolController.DefaultProtocolProvider;
                var acceptProvider = ContentTypeController.ResolveOutputContentTypeProvider(RequestInternal, contentType);
                var serialized = protocolProvider.ProtocolProvider.Serialize(this, GetStream, acceptProvider);
                if (serialized is Error rr && rr._body is RESTarOutputStreamController rsc)
                    _body = rsc.Stream;
                if (_body?.CanRead == true)
                {
                    if (_body.Length == 0)
                    {
                        _body.Dispose();
                        _body = null;
                    }
                    else if (Headers.ContentType == null)
                        Headers.ContentType = acceptProvider.ContentType;
                }
                else _body = null;
                return serialized;
            }
            catch (Exception exception)
            {
                _body?.Dispose();
                return GetResult(exception, RequestInternal).Serialize();
            }
            finally
            {
                IsSerializing = false;
                IsSerialized = true;
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
                Headers["RESTar-elapsed-ms"] = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal static IResult GetResult(Exception exs, IRequestInternal request)
        {
            var error = GetError(exs);
            error.SetTrace(request);
            error.RequestInternal = request;
            string errorId = null;
            if (!(error is Forbidden) && request.Method >= 0)
            {
                Admin.Error.ClearOld();
                Db.TransactAsync(() => errorId = Admin.Error.Create(error, request).Id);
            }
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
            if (errorId != null)
                error.Headers["ErrorInfo"] = $"/restar.admin.error/id={HttpUtility.UrlEncode(errorId)}";
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