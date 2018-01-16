using System;
using RESTar.Internal;

namespace RESTar.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket, IDisposable
    {
        void Open();
        ITarget Target { get; set; }
        ITerminal Terminal { get; set; }
        TerminalResource TerminalResource { get; set; }
        DateTime Opened { get; }
        DateTime Closed { get; }
        ulong BytesReceived { get; }
        ulong BytesSent { get; }
        void HandleTextInput(string textData);
        void HandleBinaryInput(byte[] binaryData);
        void SendTextRaw(string textData);
        void EnterShell();
    }
}