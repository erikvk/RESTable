using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;

namespace RESTar.Operations
{
    /// <inheritdoc cref="IFinalizedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    public abstract class Result : IFinalizedResult, IResult
    {
        #region IResult

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; set; }

        /// <inheritdoc />
        public string StatusDescription { get; set; }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <inheritdoc />
        public ICollection<string> Cookies { get; internal set; }

        #endregion

        #region Finalized

        /// <inheritdoc />
        public Stream Body { get; set; }

        /// <inheritdoc />
        public ContentType ContentType { get; set; }

        #endregion

        #region Trace and log

        /// <inheritdoc />
        public LogEventType LogEventType => LogEventType.HttpOutput;

        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription} ({Body?.Length ?? 0} bytes)";

        /// <inheritdoc />
        public string LogContent { get; } = null;

        /// <inheritdoc />
        public TCPConnection TcpConnection { get; }

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        #endregion

        internal Result(ITraceable trace)
        {
            Headers = new Headers();
            ExcludeHeaders = false;
            TcpConnection = trace?.TcpConnection;
            TraceId = trace?.TraceId;
        }
    }
}