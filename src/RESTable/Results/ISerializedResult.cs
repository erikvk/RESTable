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

        public long EntityCount { get; set; }

        public bool IsPaged => EntityCount > 0 && EntityCount == (long?) Result.Request?.MetaConditions.Limit;

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

        /// <summary>
        /// The number of entities in the collection. Should be set by the serializer, since it is unknown
        /// until the collection is iterated.
        /// </summary>
        long EntityCount { get; set; }

        /// <summary>
        /// Is this result paged?
        /// </summary>
        bool IsPaged { get; }
    }
}