using System;
using System.IO;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results
{
    public class SerializedResult : ISerializedResult
    {
        public RESTableContext Context => Result.Context;

        public long EntityCount { get; set; }

        public bool HasNextPage => EntityCount > 0 && EntityCount == (long?) Result.Request?.MetaConditions.Limit;
        public bool HasPreviousPage => EntityCount > 0 && Result.Request?.MetaConditions.Offset > 0;

        public MessageType MessageType => Result.MessageType;
        public ValueTask<string> GetLogMessage() => Result.GetLogMessage();
        public async ValueTask<string> GetLogContent() => await Body.ToStringAsync().ConfigureAwait(false);
        public DateTime LogTime => Result.LogTime;

        public IResult Result { get; }
        public Body Body { get; }
        public TimeSpan TimeElapsed => Result.TimeElapsed;

        public SerializedResult(IResult result, Stream customOutputStream = null)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Body = result.ProtocolHolder != null ? Body.CreateOutputBody(result.ProtocolHolder, customOutputStream) : null;
        }

        public void Dispose()
        {
            Result.Dispose();
            Body?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Result.DisposeAsync();
            if (Body == null) return;
            await Body.DisposeAsync().ConfigureAwait(false);
        }
    }
}