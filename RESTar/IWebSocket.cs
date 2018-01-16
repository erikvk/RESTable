using System.IO;
using RESTar.Operations;
using RESTar.Requests;

namespace RESTar
{
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
        /// Sends a result over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        void SendResult(IFinalizedResult result);

        /// <summary>
        /// Sends an object over the WebSocket, serialized as JSON text
        /// </summary>
        void SendJson(object item);

        /// <summary>
        /// The TCP connection over which this WebSocket is facilitated
        /// </summary>
        TCPConnection TcpConnection { get; }

        /// <summary>
        /// The headers included in the initial HTTP request
        /// </summary>
        Headers Headers { get; }

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