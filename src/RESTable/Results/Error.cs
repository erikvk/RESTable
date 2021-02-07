using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using RESTable.Internal;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="Exception" />
    /// <inheritdoc cref="ITraceable" />
    /// <inheritdoc cref="ISerializedResult" />
    /// <summary>
    /// A super class for all custom RESTable exceptions
    /// </summary>
    public abstract class Error : RESTableException, IResult, ISerializedResult, ITraceable
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

        internal void SetTrace(ITraceable request)
        {
            TraceId = request.TraceId;
            Context = request.Context;
        }

        /// <inheritdoc />
        public string TraceId { get; private set; }

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
            if (info != null)
                tail += $". {info}";
            if (errorInfo != null)
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

        internal Error(ErrorCodes code, string message, Exception ie) : base(code, message, ie)
        {
            ExcludeHeaders = false;
            if (message == null)
                Headers.Info = ie?.Message;
            else Headers.Info = message;
            IsSuccess = false;
        }

        /// <inheritdoc />
        public IEntities<T> ToEntities<T>() where T : class => throw this;

        /// <inheritdoc />
        public void ThrowIfError() => throw this;

        internal IRequestInternal RequestInternal { get; set; }

        private readonly string _logContent = null;

        /// <inheritdoc />
        public Body Body { get; private set; }

        /// <inheritdoc />
        public bool IsSerialized { get; private set; }

        /// <inheritdoc />
        public ISerializedResult Serialize()
        {
            if (RequestInternal == null)
            {
                IsSerialized = true;
                Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
                return this;
            }
            var stopwatch = Stopwatch.StartNew();
            ISerializedResult result = this;
            Body = Body.CreateOutputBody(RequestInternal);
            var cachedProvider = RequestInternal.CachedProtocolProvider;
            var acceptProvider = RequestInternal.GetOutputContentTypeProvider();
            try
            {
                return cachedProvider.ProtocolProvider.Serialize(this, acceptProvider);
            }
            catch (Exception exception)
            {
                result.Body?.Dispose();
                return exception.AsResultOf(RequestInternal).Serialize();
            }
            finally
            {
                IsSerialized = true;
                stopwatch.Stop();
                TimeElapsed = TimeElapsed + stopwatch.Elapsed;
                Headers.Elapsed = TimeElapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <inheritdoc />
        public virtual string Metadata => $"{GetType().Name};;";

        /// <inheritdoc />
        /// <summary>
        /// The time elapsed from the start of reqeust evaluation
        /// </summary>
        public TimeSpan TimeElapsed { get; internal set; }

        public async ValueTask DisposeAsync()
        {
            if (Body == null) return;
            Body.CanClose = true;
            await Body.DisposeAsync();
        }
    }
}