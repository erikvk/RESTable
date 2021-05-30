using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A super class for all custom RESTable exceptions
    /// </summary>
    public abstract class Error : RESTableException, IResult, ITraceable
    {
        /// <inheritdoc />
        public HttpStatusCode StatusCode { get; protected set; }

        /// <inheritdoc />
        public string StatusDescription { get; protected set; }

        /// <inheritdoc />
        public Headers Headers { get; } = new();

        /// <inheritdoc />
        public Cookies Cookies => Context.Client.Cookies;

        /// <inheritdoc />
        public bool IsSuccess { get; }

        /// <inheritdoc />
        public bool IsError => !IsSuccess;

        #region ITraceable, ILogable

        internal void SetContext(RESTableContext context)
        {
            Context = context;
        }

        /// <inheritdoc />
        public RESTableContext Context { get; private set; }

        /// <inheritdoc />
        public MessageType MessageType => MessageType.HttpOutput;

        /// <inheritdoc />
        public ValueTask<string> GetLogMessage()
        {
            var info = Headers.Info;
            var errorInfo = Headers.Error;
            var tail = "";
            if (info is not null)
                tail += $". {info}";
            if (errorInfo is not null)
                tail += $" (see {errorInfo})";
            return new ValueTask<string>($"{StatusCode.ToCode()}: {StatusDescription}{tail}");
        }

        /// <inheritdoc />
        public ValueTask<string> GetLogContent() => new(_logContent);

        /// <inheritdoc />
        public DateTime LogTime { get; } = DateTime.Now;

        /// <inheritdoc />
        public string HeadersStringCache { get; set; }

        /// <inheritdoc />
        public bool ExcludeHeaders { get; }

        #endregion

        internal Error(ErrorCodes code, string message) : base(code, message)
        {
            ExcludeHeaders = false;
            Headers.Info = Message;
            IsSuccess = false;
        }

        internal Error(ErrorCodes code, string? message, Exception? ie) : base(code, message, ie)
        {
            ExcludeHeaders = false;
            if (message is null)
                Headers.Info = ie?.Message;
            else Headers.Info = message;
            IsSuccess = false;
        }

        /// <inheritdoc />
        public IEntities<T> ToEntities<T>() where T : class => throw this;

        /// <inheritdoc />
        public void ThrowIfError() => throw this;

        /// <inheritdoc />
        public IProtocolHolder ProtocolHolder => Request;

        /// <inheritdoc />
        public IRequest Request { get; set; }

        /// <inheritdoc />
        public void Dispose() => Request?.Body?.Dispose();

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (Request?.Body is Body body)
                await body.DisposeAsync().ConfigureAwait(false);
        }

        private readonly string _logContent = null;

        /// <inheritdoc />
        public virtual string Metadata => $"{GetType().Name};;";

        /// <inheritdoc />
        /// <summary>
        /// The time elapsed from the start of reqeust evaluation
        /// </summary>
        public TimeSpan TimeElapsed => Request?.TimeElapsed ?? default;
    }
}