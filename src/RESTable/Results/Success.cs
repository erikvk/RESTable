using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results
{
    /// <inheritdoc cref="IResult"/>
    public abstract class Success : IResult
    {
        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public RESTableContext Context { get; }

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public Headers Headers { get; }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public abstract IRequest Request { get; }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public IProtocolHolder ProtocolHolder { get; }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public Cookies Cookies => Context.Client.Cookies;

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public TimeSpan TimeElapsed { get; set; }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public virtual MessageType MessageType => MessageType.HttpOutput;

        /// <inheritdoc />
        public virtual ValueTask<string> GetLogMessage() => new($"{StatusCode.ToCode()}: {StatusDescription}");

        public ValueTask<string> GetLogContent() => new(default(string));

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public bool IsSuccess { get; }

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public bool IsError => !IsSuccess;

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public bool ExcludeHeaders { get; }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public DateTime LogTime { get; }

        /// <inheritdoc />
        public virtual IEntities<T> ToEntities<T>() where T : class => (Entities<T>) this;

        /// <inheritdoc />
        public void ThrowIfError() { }

        protected Success(IProtocolHolder protocolHolder, Headers headers = null)
        {
            ProtocolHolder = protocolHolder;
            Context = protocolHolder.Context;
            ExcludeHeaders = false;
            Headers = headers ?? new Headers();
            LogTime = DateTime.Now;
            IsSuccess = true;
        }

        /// <inheritdoc />
        [RESTableMember(hide: true)]
        public virtual string Metadata => $"{GetType().Name};;";
    }
}