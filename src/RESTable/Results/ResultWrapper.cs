using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Wraps a result and maps operations to its members
    /// </summary>
    public abstract class ResultWrapper : IResult
    {
        /// <inheritdoc />
        public RESTableContext Context => Result.Context;

        /// <inheritdoc />
        public MessageType MessageType => Result.MessageType;

        /// <inheritdoc />
        public ValueTask<string> GetLogMessage() => Result.GetLogMessage();

        /// <inheritdoc />
        public ValueTask<string> GetLogContent() => Result.GetLogContent();

        /// <inheritdoc />
        public Headers Headers => Result.Headers;

        /// <inheritdoc />
        public string? HeadersStringCache
        {
            get => Result.HeadersStringCache;
            set => Result.HeadersStringCache = value;
        }

        /// <inheritdoc />
        public bool IsSuccess => Result.IsSuccess;

        /// <inheritdoc />
        public bool IsError => Result.IsError;

        /// <inheritdoc />
        public bool ExcludeHeaders => Result.ExcludeHeaders;

        /// <inheritdoc />
        public DateTime LogTime => Result.LogTime;

        /// <inheritdoc />
        public HttpStatusCode StatusCode => Result.StatusCode;

        /// <inheritdoc />
        public string StatusDescription => Result.StatusDescription;

        /// <inheritdoc />
        public Cookies Cookies => Result.Cookies;

        /// <inheritdoc />
        public void ThrowIfError() => Result.ThrowIfError();

        /// <inheritdoc />
        public IEntities<T> ToEntities<T>() where T : class => Result.ToEntities<T>();

        /// <inheritdoc />
        public TimeSpan TimeElapsed => Result.TimeElapsed;

        /// <inheritdoc />
        public IRequest Request => Result.Request;

        /// <inheritdoc />
        public IProtocolHolder ProtocolHolder => Result.ProtocolHolder;

        /// <inheritdoc />
        public string Metadata => Result.Metadata;

        /// <inheritdoc />
        public void Dispose() => Result.Dispose();

        /// <inheritdoc />
        public ValueTask DisposeAsync() => Result.DisposeAsync();

        /// <summary>
        /// The wrapped result
        /// </summary>
        private IResult Result { get; }

        protected ResultWrapper(IResult result) => Result = result;
    }
}