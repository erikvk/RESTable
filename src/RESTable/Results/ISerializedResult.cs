using System;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results
{
    internal class SerializedResult : ISerializedResult
    {
        public string TraceId => Result.TraceId;
        public RESTableContext Context => Result.Context;
        public Headers Headers => Result.Headers;

        public string HeadersStringCache
        {
            get => Result.HeadersStringCache;
            set => Result.HeadersStringCache = value;
        }

        public bool ExcludeHeaders => Result.ExcludeHeaders;
        public MessageType MessageType => Result.MessageType;
        public ValueTask<string> GetLogMessage() => Result.GetLogMessage();
        public async ValueTask<string> GetLogContent() => await Body.ToStringAsync();
        public DateTime LogTime => Result.LogTime;

        public IResult Result { get; }
        public Body Body { get; }
        public TimeSpan TimeElapsed => Result.TimeElapsed;

        public SerializedResult(IResult result)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Body = result.ProtocolHolder != null ? Body.CreateOutputBody(result.ProtocolHolder) : null;
        }

        public void Dispose()
        {
            if (Body == null) return;
            Body.CanClose = true;
            Body.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Body == null) return;
            Body.CanClose = true;
            await Body.DisposeAsync();
        }
    }

    public interface ISerializedResult : ILogable, ITraceable, IHeaderHolder, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The result that was serialized
        /// </summary>
        IResult Result { get; }

        /// <summary>
        /// The serialized body contained in the result. Can be seekable or non-seekable.
        /// </summary>
        Body Body { get; }

        /// <summary>
        /// The time it took for RESTable to generate and serialize the result.
        /// </summary>
        TimeSpan TimeElapsed { get; }
    }
}