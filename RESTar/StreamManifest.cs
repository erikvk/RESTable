using System;
using RESTar.Results.Success;

#pragma warning disable 414

namespace RESTar
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

        public long TotalLength;
        public long BytesRemaining;
        public long BytesStreamed;

        public int NrOfMessages;
        public int MessagesRemaining;
        public int MessagesStreamed;

        public readonly string ContentType;
        public readonly string EntityType;
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

        internal StreamManifest(Content content)
        {
            Content = content;
            ContentType = content.ContentType.ToString();
            EntityType = content.EntityType.FullName;
            EntityCount = content.EntityCount;
            TotalLength = content.Body.Length;
            BytesRemaining = content.Body.Length;
        }

        public void Dispose() => Content.Body.Dispose();
    }
}