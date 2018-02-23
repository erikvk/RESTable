using System;
using RESTar.Internal;

namespace RESTar.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket, IDisposable
    {
        void Open();
        ITerminal Terminal { get; set; }
        ITerminalResource TerminalResource { get; set; }
        DateTime Opened { get; }
        DateTime Closed { get; }
        ulong BytesReceived { get; }
        ulong BytesSent { get; }
        void HandleTextInput(string textData);
        void HandleBinaryInput(byte[] binaryData);
        void SendTextRaw(string textData);
        void Disconnect();
        ConnectionProfile GetConnectionProfile();
        string AuthToken { get; set; }
    }
}