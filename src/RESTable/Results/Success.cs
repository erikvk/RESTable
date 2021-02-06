using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="IResult"/>
    /// <inheritdoc cref="ISerializedResult"/>
    public abstract class Success : IResult, ISerializedResult
    {
        private string _logContent;

        /// <inheritdoc />
        public string TraceId { get; }

        /// <inheritdoc />
        public RESTableContext Context { get; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public virtual Headers Headers { get; }

        /// <inheritdoc />
        public Cookies Cookies => Context.Client.Cookies;

        /// <inheritdoc />
        public bool IsSerialized { get; protected set; }

        public virtual Body Body => null;

        /// <inheritdoc />
        public virtual ISerializedResult Serialize()
        {
            IsSerialized = true;
            return this;
        }

        /// <inheritdoc />
        public TimeSpan TimeElapsed { get; protected set; }

        /// <inheritdoc />
        public virtual MessageType MessageType => MessageType.HttpOutput;

        /// <inheritdoc />
        public virtual ValueTask<string> GetLogMessage() => new($"{StatusCode.ToCode()}: {StatusDescription} ({Body?.ContentLength ?? 0} bytes)");

        public string LogContent
        {
            get => _logContent;
            protected set => _logContent = value;
        }

        public ValueTask<string> GetLogContent() => new(_logContent);

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool IsSuccess { get; }

        /// <inheritdoc />
        public bool IsError => !IsSuccess;

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        /// <inheritdoc />
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public virtual IEntities<T> ToEntities<T>() where T : class => (Entities<T>) this;

        /// <inheritdoc />
        public void ThrowIfError() { }

        protected Success(ITraceable trace)
        {
            TraceId = trace.TraceId;
            Context = trace.Context;
            ExcludeHeaders = false;
            Headers = new Headers();
            IsSerialized = false;
            LogTime = DateTime.Now;
            IsSuccess = true;
        }

        /// <inheritdoc />
        public virtual string Metadata => $"{GetType().Name};;";

        public async ValueTask DisposeAsync()
        {
            if (Body == null) return;
            Body.CanClose = true;
            await Body.DisposeAsync();
        }
    }
}