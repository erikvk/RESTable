using System;
using RESTar.Internal;
using RESTar.Operations;

namespace RESTar.WebSockets
{
    internal interface IWebSocketInternal : IWebSocket, IDisposable
    {
        void Open();
        ITerminal Terminal { get; set; }
        TerminalResource TerminalResource { get; set; }
        DateTime Opened { get; }
        DateTime Closed { get; }
        ulong BytesReceived { get; }
        ulong BytesSent { get; }
        void HandleTextInput(string textData);
        void HandleBinaryInput(byte[] binaryData);
        void SendTextRaw(string textData);
        void Disconnect();

        /// <summary>
        /// Sends a result over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendResult(IFinalizedResult result, bool includeStatusWithContent = true);

        ConnectionProfile GetConnectionProfile();
    }
}