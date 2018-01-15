using System.IO;

namespace RESTar
{
    /// <summary>
    /// The status of a RESTar WebSocket connection
    /// </summary>
    public enum WebSocketStatus
    {
        /// <summary>
        /// The WebSocket is closed. No messages can be sent or received
        /// </summary>
        Closed,

        /// <summary>
        /// The WebSocket is open. Messages can be sent and received.
        /// </summary>
        Open,

        /// <summary>
        /// The WebSocket is pending and will soon open. Sent messages will be queued until the WebSocket
        /// is opened. Messages cannot be received.
        /// </summary>
        PendingOpen,

        /// <summary>
        /// The WebSocket is pending and will soon close. Messages cannot be sent. Disconnect calls are
        /// ignored.
        /// </summary>
        PendingClose
    }

    /// <summary>
    /// WebSockets support sending continuous data over a single TCP connection
    /// </summary>
    public interface IWebSocket
    {
        /// <summary>
        /// Gets a unique ID of the WebSocket.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Sends an object over the WebSocket, serialized as JSON text
        /// </summary>
        void SendJson(object items);

        /// <summary>
        /// Sends the string data as text over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendText(string data);

        /// <summary>
        /// Sends the byte array data as text over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendText(byte[] data);

        /// <summary>
        /// Sends the Stream data as text over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendText(Stream data);

        /// <summary>
        /// Sends the byte array data as binary over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendBinary(byte[] data);

        /// <summary>
        /// Sends the Stream data as binary over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendBinary(Stream data);

        /// <summary>
        /// Disconnects the WebSocket
        /// </summary>
        void Disconnect();

        /// <summary>
        /// The current status of this WebSocket
        /// </summary>
        WebSocketStatus Status { get; }
    }
}