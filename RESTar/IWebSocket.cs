using System.IO;
using RESTar.Operations;

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
        /// The WebSocket is pending. Send messages will be queued until the WebSocket
        /// is opened. Messages cannot be received.
        /// </summary>
        Pending
    }

    /// <summary>
    /// WebSockets support sending continuous data over a single TCP connection
    /// </summary>
    public interface IWebSocket
    {
        /// <summary>
        /// The action to perform when receiving string data over the WebSocket
        /// </summary>
        WebSocketReceiveAction InputHandler { set; }

        /// <summary>
        /// The action to perform when disconnecting the WebSocket
        /// </summary>
        WebSocketDisconnectAction DisconnectHandler { set; }

        /// <summary>
        /// Gets a unique ID of the WebSocket.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Sends the string data over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        /// <param name="data"></param>
        void Send(string data);

        /// <summary>
        /// Sends the Stream data over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        /// <param name="data"></param>
        void Send(Stream data);

        /// <summary>
        /// Disconnects the WebSocket
        /// </summary>
        void Disconnect();

        /// <summary>
        /// The current status of this WebSocket
        /// </summary>
        WebSocketStatus Status { get; }

        /// <summary>
        /// Should the output from regular RESTar operations be ignored?
        /// </summary>
        bool IgnoreOutput { get; set; }

        /// <summary>
        /// The current location within the API, where the WebSocket is located
        /// </summary>
        string CurrentLocation { get; }
    }
}