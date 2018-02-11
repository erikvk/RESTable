using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;

namespace RESTar.Operations
{
    /// <inheritdoc />
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    public abstract class Result : IFinalizedResult
    {
        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; set; }

        /// <inheritdoc />
        public string StatusDescription { get; set; }

        /// <summary>
        /// The body to use in HTTP responses based on this result
        /// </summary>
        public MemoryStream Body { get; set; }

        Stream IFinalizedResult.Body => Body;

        /// <inheritdoc />
        public string ContentType { get; set; }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <inheritdoc />
        public ICollection<string> Cookies { get; internal set; }

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

        internal Result(ITraceable trace)
        {
            Headers = new Headers();
            ExcludeHeaders = false;
            TcpConnection = trace?.TcpConnection;
            TraceId = trace?.TraceId;
        }
    }
}