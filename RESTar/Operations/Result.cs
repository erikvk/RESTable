using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;

namespace RESTar.Operations
{
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    internal abstract class Result : IFinalizedResult
    {
        /// <summary>
        /// The status code to use in HTTP responses based on this result
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The status description to use in HTTP responses based on this result
        /// </summary>
        public string StatusDescription { get; set; }

        /// <summary>
        /// The body to use in HTTP responses based on this result
        /// </summary>
        public MemoryStream Body { get; set; }

        Stream IFinalizedResult.Body => Body;

        /// <summary>
        /// The content type to use in HTTP responses based on this result
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The headers to use in HTTP responses based on this result
        /// </summary>
        public Headers Headers { get; }

        /// <summary>
        /// The cookies to set in the response
        /// </summary>
        public ICollection<string> Cookies { get; internal set; }

        public LogEventType LogEventType => LogEventType.HttpOutput;
        public string TraceId { get; }
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription} ({Body?.Length ?? 0} bytes)";
        public string LogContent { get; } = null;
        public TCPConnection TcpConnection { get; }
        public string HeadersStringCache { get; set; }
        public bool ExcludeHeaders { get; }

        internal Result(ITraceable trace)
        {
            Headers = new Headers();
            ExcludeHeaders = false;
            TcpConnection = trace.TcpConnection;
            TraceId = trace.TraceId;
        }
    }
}