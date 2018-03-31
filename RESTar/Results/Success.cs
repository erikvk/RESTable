using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using RESTar.Logging;
using RESTar.Requests;

namespace RESTar.Results
{
    /// <inheritdoc cref="ISerializedResult"/>
    /// <inheritdoc cref="IResult"/>
    public abstract class Success : ISerializedResult, IResult
    {
        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public Context Context { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; protected set; }

        /// <inheritdoc />
        public ICollection<string> Cookies { get; internal set; }

        /// <inheritdoc />
        public virtual ISerializedResult Serialize(ContentType? contentType = null) => this;

        /// <inheritdoc />
        public virtual Stream Body { get; }

        /// <inheritdoc />
        public ContentType? ContentType { get; protected set; }

        /// <inheritdoc />
        public TimeSpan TimeElapsed { get; protected set; }

        /// <inheritdoc />
        public virtual LogEventType LogEventType => LogEventType.HttpOutput;

        /// <inheritdoc />
        public virtual string LogMessage => $"{StatusCode.ToCode()}: {StatusDescription} ({Body?.Length ?? 0} bytes)";

        /// <inheritdoc />
        public string LogContent { get; protected set; }

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public IEnumerable<T> ToEntities<T>() where T : class => (Entities<T>) this;

        /// <inheritdoc />
        public void ThrowIfError() { }

        /// <inheritdoc />
        protected Success(ITraceable trace)
        {
            Context = trace.Context;
            TraceId = trace.TraceId;
            ExcludeHeaders = false;
            LogTime = DateTime.Now;
            Body = null;
        }
    }
}