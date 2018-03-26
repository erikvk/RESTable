using System;
using System.Collections.Generic;
using System.IO;
using RESTar.Queries;
using RESTar.Resources;
using RESTar.WebSockets;

namespace RESTar
{
    /// <inheritdoc />
    /// <summary>
    /// WebSockets support sending continuous data over a single TCP connection
    /// </summary>
    public interface IWebSocket : ITraceable
    {
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
        /// Sends an object over the WebSocket, serialized as JSON text. The output pretty print setting is controlled by
        /// the prettyPrint parameter. If null, the global pretty print setting is used.
        /// </summary>
        void SendJson(object item, bool? prettyPrint = null, bool ignoreNulls = false);

        /// <summary>
        /// Sends a result over a WebSocket.
        /// </summary>
        /// <param name="result">The result to send</param>
        /// <param name="includeStatusWithContent">Should the result status code and description be included before the result content?</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included</param>
        void SendResult(ISerializedResult result, bool includeStatusWithContent = true, TimeSpan? timeElapsed = null);

        /// <summary>
        /// Sends an exception over the WebSocket.
        /// </summary>
        void SendException(Exception exception);

        /// <summary>
        /// The headers included in the initial HTTP request
        /// </summary>
        Headers Headers { get; }

        /// <summary>
        /// Closes the current terminal (if any) and sends the WebSocket to the Shell terminal. Use this to quit from a 
        /// terminal resource.
        /// </summary>
        void SendToShell(IEnumerable<Condition<Shell>> assignments = null);

        /// <summary>
        /// Closes the current terminal (if any) and sends the WebSocket to the provided terminal. Use this to quit from a 
        /// terminal resource and open another terminal instead.
        /// </summary>
        void SendTo<T>(ITerminalResource<T> terminalResource, IEnumerable<Condition<T>> assignments = null) where T : class, ITerminal;

        /// <summary>
        /// The current status of this WebSocket
        /// </summary>
        WebSocketStatus Status { get; }
    }
}