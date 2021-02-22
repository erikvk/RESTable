using System;
using System.IO;
using System.Threading.Tasks;

namespace RESTable.WebSockets
{
    public class WebSocketMessage : IAsyncDisposable
    {
        public Stream Data { get; }
        public bool IsText { get; }
        public bool IsBinary => !IsText;

        public ValueTask DisposeAsync() => Data.DisposeAsync();
    }
}