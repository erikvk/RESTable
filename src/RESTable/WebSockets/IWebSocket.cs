using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace RESTable.WebSockets;

/// <inheritdoc cref="RESTable.ITraceable" />
/// <inheritdoc cref="RESTable.IProtocolHolder" />
/// <summary>
///     WebSockets support sending continuous data over a single TCP connection
/// </summary>
public interface IWebSocket : ITraceable, IProtocolHolder, IAsyncDisposable
{
    /// <summary>
    ///     The cookies of the initial HTTP request
    /// </summary>
    ReadonlyCookies Cookies { get; }

    /// <summary>
    ///     The current status of this WebSocket
    /// </summary>
    WebSocketStatus Status { get; }

    /// <summary>
    ///     A description of why the WebSocket is closing (or empty string if not closing)
    /// </summary>
    string CloseDescription { get; }

    /// <summary>
    ///     A cancellation token that is cancelled when the the WebSocket has been aborted
    /// </summary>
    CancellationToken WebSocketAborted { get; }

    /// <summary>
    ///     Sends the string data as text over the WebSocket. Send calls to a closed WebSocket will be queued and sent
    ///     when the WebSocket is opened.
    /// </summary>
    Task SendText(string data, CancellationToken cancellationToken = new());

    /// <summary>
    ///     Sends the buffered data over the websocket as either text or binary
    /// </summary>
    Task Send(ReadOnlyMemory<byte> data, bool asText, CancellationToken cancellationToken = new());

    /// <summary>
    ///     Returns a stream that, when written to, writes data over the websocket over a single message until the stream is
    ///     disposed
    /// </summary>
    ValueTask<Stream> GetMessageStream(bool asText, CancellationToken cancellationToken = new());

    /// <summary>
    ///     Sends a result asynchronously over a WebSocket.
    /// </summary>
    /// <param name="result">
    ///     The result to send. The body of the result (if any) will be sent as binary over the websocket.
    ///     Additional inforation can be included in separate text messages (see other parameters).
    /// </param>
    /// <param name="timeElapsed">
    ///     The elapsed time to include, or null if no time should be included. If not null, timeElapsed
    ///     will be included in the status text message (see writeStatus)
    /// </param>
    /// <param name="writeHeaders">
    ///     Should headers be included as a text message? If true, headers are printed after the status
    ///     (if any) and before the content is sent.
    /// </param>
    Task SendResult
    (
        IResult result,
        TimeSpan? timeElapsed = null,
        bool writeHeaders = false,
        CancellationToken cancellationToken = new()
    );

    /// <summary>
    ///     Sends an exception over the WebSocket.
    /// </summary>
    Task SendException(Exception exception, CancellationToken cancellationToken = new());

    /// <summary>
    ///     Closes the current terminal (if any) and directs the WebSocket to the Shell terminal. Use this to quit from a
    ///     terminal resource and launch the shell.
    /// </summary>
    Task DirectToShell(ICollection<Condition<Shell>>? assignments = null, CancellationToken cancellationToken = new());

    /// <summary>
    ///     Closes the current terminal (if any) and directs the WebSocket to the provided terminal. Use this to quit from a
    ///     terminal resource and open another terminal instead.
    /// </summary>
    Task DirectTo<T>(ITerminalResource<T> terminalResource, ICollection<Condition<T>>? assignments = null, CancellationToken cancellationToken = new())
        where T : Terminal;
}
