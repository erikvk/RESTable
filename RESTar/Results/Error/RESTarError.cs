using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using RESTar.Internal;
using RESTar.Logging;
using RESTar.Operations;
using RESTar.Requests;
using RESTar.Results.Error.BadRequest;
using RESTar.Results.Error.Forbidden;
using RESTar.Results.Success;
using static RESTar.Operations.Transact;
using static RESTar.Methods;

namespace RESTar.Results.Error
{
    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="IFinalizedResult" />
    /// <summary>
    /// A super class for all custom RESTar exceptions
    /// </summary>
    public abstract class RESTarError : Exception, ITraceable, IFinalizedResult
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

        internal void SetTrace(ITraceable trace)
        {
            TraceId = trace.TraceId;
            TcpConnection = trace.TcpConnection;
        }

        /// <inheritdoc />
        public string TraceId { get; private set; }

        /// <inheritdoc />
        public TCPConnection TcpConnection { get; private set; }

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
        public ContentType ContentType { get; } = null;

        internal static RESTarError GetError(Exception exception)
        {
            switch (exception)
            {
                case RESTarError re: return re;
                case FormatException _: return new UnsupportedContent(exception);
                case JsonReaderException _: return new FailedJsonDeserialization(exception);
                case Starcounter.DbException _ when exception.Message.Contains("SCERR4034"): return new AbortedByCommitHook(exception);
                case Starcounter.DbException _: return new StarcounterDatabaseError(exception);
                case RuntimeBinderException _: return new BinderPermissions(exception);
                default: return new Unknown(exception);
            }
        }

        internal static IFinalizedResult GetResult(Exception exs, Methods method, Context context, TCPConnection tcpConnection, bool isWebSocketUpgrade)
        {
            var error = GetError(exs);
            error.SetTrace(tcpConnection);
            string errorId = null;
            if (!(error is Forbidden.Forbidden))
            {
                Admin.Error.ClearOld();
                errorId = Trans(() => Admin.Error.Create(error, context)).Id;
            }
            if (isWebSocketUpgrade)
            {
                tcpConnection.WebSocket?.SendResult(error);
                return new WebSocketResult(leaveOpen: false, trace: error);
            }
            switch (method)
            {
                case GET:
                case POST:
                case PATCH:
                case PUT:
                case DELETE:
                case REPORT:
                case HEAD:
                    if (errorId != null)
                        error.Headers["ErrorInfo"] = $"/{typeof(Admin.Error).FullName}/id={HttpUtility.UrlEncode(errorId)}";
                    return error;
                case OPTIONS:
                    return new InvalidOrigin();
                default: throw new Exception();
            }
        }

        /// <inheritdoc />
        public override string ToString() => LogMessage;
    }
}