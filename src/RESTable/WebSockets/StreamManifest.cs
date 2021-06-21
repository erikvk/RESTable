using System;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Resources;
using RESTable.Results;

#pragma warning disable 414

namespace RESTable.WebSockets
{
    internal class StreamManifest : IDisposable, IAsyncDisposable
    {
        internal ISerializedResult Result { get; }

        internal int CurrentMessageIndex { get; set; }
        internal int BufferSize { get; }
        internal int LastIndex { get; }
        public long TotalLength { get; }
        public long BytesRemaining { get; internal set; }
        public long BytesStreamed { get; internal set; }
        public int NrOfMessages { get; }
        public int MessagesRemaining { get; internal set; }
        public int MessagesStreamed { get; internal set; }
        public string ContentType { get; }

        [RESTableMember(hideIfNull: true)] 
        public string? EntityType { get; }
        
        public long EntityCount { get; }

        public StreamManifestMessage[] Messages { get; }
        public StreamCommand[] Commands => _Commands;

        private static readonly StreamCommand[] _Commands =
        {
            new() {Command = "GET", Description = "Streams all messages"},
            new() {Command = "NEXT", Description = "Streams the next message"},
            new() {Command = "NEXT <integer>", Description = "Streams the next <n> messages where <n> is an integer"},
            new() {Command = "MANIFEST", Description = "Prints the manifest"},
            new() {Command = "CLOSE", Description = "Closes the stream and returns to the previous terminal resource"}
        };

        internal StreamManifest(ISerializedResult serializedContent, int bufferSize)
        {
            Result = serializedContent;
            var content = serializedContent.Result;
            ContentType = content.Headers.ContentType!.Value.ToString();
            TotalLength = serializedContent.Body.Length;
            BufferSize = bufferSize;
            BytesRemaining = serializedContent.Body.Length;
            if (content is IEntities entities)
            {
                EntityType = entities.EntityType.GetRESTableTypeName();
            }
            EntityCount = serializedContent.EntityCount;
            var dataLength = serializedContent.Body.Length;
            var nrOfMessages = dataLength / bufferSize;
            var last = dataLength % bufferSize;
            if (last > 0) nrOfMessages += 1;
            else last = bufferSize;
            var messages = new StreamManifestMessage[nrOfMessages];
            long startIndex = 0;
            for (var i = 0; i < messages.Length; i += 1)
            {
                messages[i] = new StreamManifestMessage
                {
                    StartIndex = startIndex,
                    Length = bufferSize
                };
                startIndex += bufferSize;
            }
            messages.Last().Length = last;
            NrOfMessages = (int) nrOfMessages;
            MessagesRemaining = (int) nrOfMessages;
            LastIndex = NrOfMessages - 1;
            Messages = messages;
        }

        public void Dispose() => Result.Dispose();
        public async ValueTask DisposeAsync() => await Result.DisposeAsync().ConfigureAwait(false);
    }
}