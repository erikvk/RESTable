using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;

namespace RESTable.AspNetCore
{
    internal abstract class AspNetCoreMessageStream : Stream, IAsyncDisposable
    {
        protected WebSocket WebSocket { get; }
        internal WebSocketMessageType MessageType { get; }
        protected CancellationToken WebSocketCancelledToken { get; }
        protected bool IsDisposed { get; set; }
        public override void Flush() { }
        public override bool CanSeek => false;
        protected int ByteCount { get; set; }

        public override long Position
        {
            get => ByteCount;
            set => throw new NotSupportedException();
        }

        protected AspNetCoreMessageStream(WebSocket webSocket, WebSocketMessageType messageType, CancellationToken webSocketCancelledToken)
        {
            webSocketCancelledToken.ThrowIfCancellationRequested();
            WebSocketCancelledToken = webSocketCancelledToken;
            WebSocket = webSocket;
            MessageType = messageType;
        }

#if NETSTANDARD2_0
        public virtual System.Threading.Tasks.ValueTask DisposeAsync()
        {
            return default;
        }
#endif

        #region Unsupported

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override long Length => throw new NotSupportedException();

        #endregion
    }
}