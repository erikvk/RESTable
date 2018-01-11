using System;
using System.IO;

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
        Action<string> OnReceive { get; set; }

        /// <summary>
        /// Gets a unique ID of the WebSocket
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Sends the byte array data over the WebSocket
        /// </summary>
        /// <param name="data"></param>
        void Send(byte[] data);

        /// <summary>
        /// Sends the string data over the WebSocket
        /// </summary>
        /// <param name="data"></param>
        void Send(string data);

        /// <summary>
        /// Sends the Stream data over the WebSocket
        /// </summary>
        /// <param name="data"></param>
        void Send(Stream data);

        /// <summary>
        /// Disconnects the WebSocket
        /// </summary>
        void Disconnect();
    }
}