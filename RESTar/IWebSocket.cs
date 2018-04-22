using System;
using System.Collections.Generic;
using RESTar.Requests;
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
        /// Sends the byte array data as text over the WebSocket.
        /// </summary>
        void SendText(byte[] data, int offset, int length);

        /// <summary>
        /// Sends the byte array data as binary over the WebSocket.
        /// </summary>
        void SendBinary(byte[] data, int offset, int length);

        /// <summary>
        /// Sends an object over the WebSocket, serialized as JSON text. The output pretty print setting is controlled by
        /// the prettyPrint parameter. If null, the global pretty print setting is used.
        /// </summary>
        void SendJson(object item, bool asText = false, bool? prettyPrint = null, bool ignoreNulls = false);

        /// <summary>
        /// Sends a result over a WebSocket.
        /// </summary>
        /// <param name="result">The result to send. The body of the result will be sent as binary over the websocket.
        /// Additional inforation can be included in separate text messages (see other parameters).</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included. If not null, timeElapsed 
        /// will be included in the status text message (see writeStatus)</param>
        /// <param name="writeHeaders">Should headers be included as a text message? If true, headers are printed after the status
        /// (if any) and before the content is sent.</param>
        /// <param name="disposeResult">Should the result be disposed after it is sent to the WebSocket?</param>
        void SendResult(ISerializedResult result, TimeSpan? timeElapsed = null, bool writeHeaders = false, bool disposeResult = true);

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