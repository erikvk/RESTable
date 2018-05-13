using System;
using System.Linq;
using Newtonsoft.Json;
using RESTar.Results;

#pragma warning disable 414

namespace RESTar.WebSockets
{
    internal class StreamManifestMessage
    {
        public long StartIndex;
        public long Length;
        public bool Sent;
    }

    internal class StreamCommand
    {
        public string Command;
        public string Description;
    }

    internal class StreamManifest : IDisposable
    {
        internal readonly Content Content;
        internal int CurrentMessageIndex;
        internal int BufferSize;

        public long TotalLength;
        public long BytesRemaining;
        public long BytesStreamed;

        public int NrOfMessages;
        public int MessagesRemaining;
        public int MessagesStreamed;

        public readonly string ContentType;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly string EntityType;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public readonly ulong EntityCount;

        public StreamManifestMessage[] Messages;
        public readonly StreamCommand[] Commands = _Commands;

        private static readonly StreamCommand[] _Commands =
        {
            new StreamCommand
            {
                Command = "GET",
                Description = "Streams all messages"
            },
            new StreamCommand
            {
                Command = "NEXT",
                Description = "Streams the next message"
            },
            new StreamCommand
            {
                Command = "NEXT <integer>",
                Description = "Streams the next <n> messages where <n> is an integer"
            },
            new StreamCommand
            {
                Command = "MANIFEST",
                Description = "Prints the manifest"
            },
            new StreamCommand
            {
                Command = "CLOSE",
                Description = "Closes the stream and returns to the shell"
            }
        };

        internal StreamManifest(Content content, int bufferSize)
        {
            Content = content;
            ContentType = content.Headers.ContentType?.ToString();
            TotalLength = content.Body.Length;
            BufferSize = bufferSize;
            BytesRemaining = content.Body.Length;
            if (content is IEntities entities)
            {
                EntityType = entities.EntityType.FullName;
                EntityCount = entities.EntityCount;
            }
            var dataLength = content.Body.Length;
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
            Messages = messages;
        }

        public void Dispose() => Content.Body.Dispose();
    }
}