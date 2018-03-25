using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;
using RESTar.Results.Success;

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

        /// <inheritdoc />
        public virtual IFinalizedResult FinalizeResult(ContentType? contentType = null) => this;

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>()
        {
            var entities = (Entities) this;
            return entities.Cast<T>();
        }

        /// <inheritdoc />
        public void ThrowIfError() { }

        #endregion

        #region Finalized

        /// <inheritdoc />
        public Stream Body { get; set; }

        /// <inheritdoc />
        public ContentType? ContentType { get; set; }

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
        public Context Context { get; }

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        /// <inheritdoc />
        public abstract TimeSpan TimeElapsed { get; protected set; }

        #endregion

        internal Result(ITraceable request)
        {
            Headers = new Headers();
            ExcludeHeaders = false;
            Context = request.Context;
            TraceId = request.TraceId;
        }
    }
}