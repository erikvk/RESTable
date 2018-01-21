using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;

namespace RESTar.Operations
{
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    internal abstract class Result : IFinalizedResult, ILogable
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
        public string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription}";
        public string LogContent { get; } = null;
        public TCPConnection TcpConnection { get; }
        private string _headersString;
        string ILogable.CustomHeadersString => _headersString ?? (_headersString = string.Join(", ", Headers.Select(p => $"{p.Key}: {p.Value}")));

        internal Result(ITraceable trace)
        {
            Headers = new Headers();
            TcpConnection = trace.TcpConnection;
            TraceId = trace.TraceId;
        }
    }
}