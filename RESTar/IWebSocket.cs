using System.IO;
using RESTar.Operations;

namespace RESTar
{
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
        /// Is the WebSocket open? Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        bool IsOpen { get; }
    }
}