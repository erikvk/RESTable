using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;
using RESTar.Results.Success;

namespace RESTar.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The result of a RESTar request operation
    /// </summary>
    public abstract class Result : ISerializedResult, IResult
    {
        #region IResult

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; }

        /// <inheritdoc />
        public ICollection<string> Cookies { get; internal set; }

        /// <inheritdoc />
        public virtual ISerializedResult Serialize(ContentType? contentType = null) => this;

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>() where T : class => (Entities<T>) this;

        /// <inheritdoc />
        public void ThrowIfError() { }

        #endregion

        #region Serialized

        /// <inheritdoc />
        public Stream Body { get; protected set; }

        /// <inheritdoc />
        public ContentType? ContentType { get; protected set; }

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

        internal Result(ITraceable trace)
        {
            Headers = new Headers();
            ExcludeHeaders = false;
            Context = trace.Context;
            TraceId = trace.TraceId;
        }
    }
}