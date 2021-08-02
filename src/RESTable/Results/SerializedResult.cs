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
        public async ValueTask<string?> GetLogContent() => await Body.ToStringAsync().ConfigureAwait(false);
        public DateTime LogTime => Result.LogTime;

        public IResult Result { get; }
        public Body Body { get; }
        public TimeSpan TimeElapsed => Result.TimeElapsed;

        public SerializedResult(IResult result, Stream customOutputStream = null)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            if (result.ProtocolHolder is null)
            {
                throw new ArgumentNullException
                (
                    paramName: nameof(IResult.ProtocolHolder),
                    message: $"Could not serialize a result of type '{result.GetType().GetRESTableTypeName()}' that " +
                             $"did not belong to a request, and hence had no information about content types"
                );
            }
            Body = Body.CreateOutputBody(result.ProtocolHolder, customOutputStream);
        }

        public void Dispose()
        {
            Result.Dispose();
            Body?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await Result.DisposeAsync().ConfigureAwait(false);
            if (Body is null) return;
            await Body.DisposeAsync().ConfigureAwait(false);
        }
    }
}