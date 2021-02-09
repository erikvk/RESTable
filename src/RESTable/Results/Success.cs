using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="IResult"/>
    public abstract class Success : IResult
    {
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
        public virtual IRequest Request { get; }

        /// <inheritdoc />
        public IProtocolHolder ProtocolHolder { get; }

        /// <inheritdoc />
        public Cookies Cookies => Context.Client.Cookies;
        
        /// <inheritdoc />
        public TimeSpan TimeElapsed { get; set; }

        /// <inheritdoc />
        public virtual MessageType MessageType => MessageType.HttpOutput;

        /// <inheritdoc />
        public virtual ValueTask<string> GetLogMessage() => new($"{StatusCode.ToCode()}: {StatusDescription}");

        public ValueTask<string> GetLogContent() => new(default(string));

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

        protected Success(IProtocolHolder protocolHolder)
        {
            ProtocolHolder = protocolHolder;
            TraceId = protocolHolder.TraceId;
            Context = protocolHolder.Context;
            ExcludeHeaders = false;
            Headers = new Headers();
            LogTime = DateTime.Now;
            IsSuccess = true;
        }

        /// <inheritdoc />
        public virtual string Metadata => $"{GetType().Name};;";
    }
}