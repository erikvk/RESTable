using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Returned when a WebSocket upgrade was performed successfully, and RESTable has already sent
///     the response back and closed the socket.
/// </summary>
public class WebSocketTransferSuccess : OK
{
    internal WebSocketTransferSuccess(IResult result, IRequest request) : base(request)
    {
        Result = result;
    }

    public IResult Result { get; }
}