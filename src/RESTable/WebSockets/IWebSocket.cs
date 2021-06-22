using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets
{
    /// <inheritdoc cref="RESTable.ITraceable" />
    /// <inheritdoc cref="RESTable.IProtocolHolder" />
    /// <summary>
    /// WebSockets support sending continuous data over a single TCP connection
    /// </summary>
    public interface IWebSocket : ITraceable, IProtocolHolder, IAsyncDisposable
    {
        /// <summary>
        /// Sends the string data as text over the WebSocket. Send calls to a closed WebSocket will be queued and sent 
        /// when the WebSocket is opened.
        /// </summary>
        Task SendText(string data, CancellationToken cancellationToken = new());

        /// <summary>
        /// Sends the byte array data as text over the WebSocket.
        /// </summary>
        Task SendText(ArraySegment<byte> buffer, CancellationToken cancellationToken = new());

        /// <summary>
        /// Sends the stream data as text over the WebSocket.
        /// </summary>
        Task SendText(Stream stream, CancellationToken cancellationToken = new());

        /// <summary>
        /// Sends the byte array data as binary over the WebSocket.
        /// </summary>
        Task SendBinary(ArraySegment<byte> buffer, CancellationToken cancellationToken = new());

        /// <summary>
        /// Sends the stream data as binary over the WebSocket.
        /// </summary>
        Task SendBinary(Stream stream, CancellationToken cancellationToken = new());

        /// <summary>
        /// Sends an object over the WebSocket, serialized as JSON text. The output pretty print setting is controlled by
        /// the prettyPrint parameter. If null, the global pretty print setting is used.
        /// </summary>
        Task SendJson
        (
            object dataObject,
            bool asText = false,
            bool? prettyPrint = null,
            bool ignoreNulls = false,
            CancellationToken cancellationToken = new()
        );

        /// <summary>
        /// Sends a result asynchronously over a WebSocket.
        /// </summary>
        /// <param name="result">The result to send. The body of the result (if any) will be sent as binary over the websocket.
        ///     Additional inforation can be included in separate text messages (see other parameters).</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included. If not null, timeElapsed 
        ///     will be included in the status text message (see writeStatus)</param>
        /// <param name="writeHeaders">Should headers be included as a text message? If true, headers are printed after the status
        ///     (if any) and before the content is sent.</param>
        Task SendResult
        (
            IResult result,
            TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            CancellationToken cancellationToken = new()
        );

        /// <summary>
        /// Sends a serialized result asynchronously over a WebSocket, with the body contained in a single binary message. If the result body is larger than
        /// 16 megabytes, a <see cref="WebSocketMessageTooLargeException"/> will be thrown. In these cases, use <see cref="StreamSerializedResult"/> instead.
        /// </summary>
        /// <param name="serializedResulte result to send. The body of the result (if any) will be sent as binary over the websocket.
        ///     Additional inforation can be included in separate text messages (see other parameters).</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included. If not null, timeElapsed 
        ///     will be included in the status text message (see writeStatus)</param>
        /// <param name="writeHeaders">Should headers be included as a text message? If true, headers are printed after the status
        ///     (if any) and before the content is sent.</param>
        /// <param name="disposeResult">Should the serialized result be disposed after it is sent to the WebSocket?</param>
        Task SendSerializedResult
        (
            ISerializedResult serializedResult,
            TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            bool disposeResult = true,
            CancellationToken cancellationToken = new()
        );

        /// <summary>
        /// Sends an arbitrarily large result over a WebSocket, with the body split over multiple binary messages. Before sending
        /// the contents of the result, RESTable will send a stream manifest as a text message, which enables a set of commands for
        /// initiating and controlling the actual data transfer. The WebSocket is directed back to the previous terminal once streaming
        /// is either completed or cancelled.
        /// </summary>
        /// <param name="serializedResult">The result to send. The body of the result will be sent as binary over the websocket.
        ///     Additional inforation can be included in separate text messages (see other parameters).</param>
        /// <param name="messageSize">The size of each message, in bytes. Cannot be less than 512 or greater than 16000000</param>
        /// <param name="timeElapsed">The elapsed time to include, or null if no time should be included. If not null, timeElapsed 
        ///     will be included in the status text message (see writeStatus)</param>
        /// <param name="writeHeaders">Should headers be included as a text message? If true, headers are printed after the status
        ///     (if any) and before the content is sent.</param>
        /// <param name="disposeResult">Should the result be disposed after it is sent to the WebSocket?</param>
        Task StreamSerializedResult
        (
            ISerializedResult serializedResult,
            int messageSize,
            TimeSpan? timeElapsed = null,
            bool writeHeaders = false,
            bool disposeResult = true,
            CancellationToken cancellationToken = new()
        );

        /// <summary>
        /// Returns a stream that, when written to, writes data over the websocket over a single message until the stream is disposed
        /// </summary>
        Task<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken = new());

        /// <summary>
        /// Sends an exception over the WebSocket.
        /// </summary>
        Task SendException(Exception exception, CancellationToken cancellationToken = new());

        /// <summary>
        /// The cookies of the initial HTTP request
        /// </summary>
        ReadonlyCookies Cookies { get; }

        /// <summary>
        /// Closes the current terminal (if any) and directs the WebSocket to the Shell terminal. Use this to quit from a 
        /// terminal resource and launch the shell.
        /// </summary>
        Task DirectToShell(ICollection<Condition<Shell>>? assignments = null, CancellationToken cancellationToken = new());

        /// <summary>
        /// Closes the current terminal (if any) and directs the WebSocket to the provided terminal. Use this to quit from a 
        /// terminal resource and open another terminal instead.
        /// </summary>
        Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>>? assignments = null, CancellationToken cancellationToken = new())
            where T : Terminal;

        /// <summary>
        /// The current status of this WebSocket
        /// </summary>
        WebSocketStatus Status { get; }
    }
}