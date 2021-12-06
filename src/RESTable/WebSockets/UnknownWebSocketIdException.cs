namespace RESTable.WebSockets;

internal class UnknownWebSocketIdException : RESTableException
{
    public UnknownWebSocketIdException(string info) : base(ErrorCodes.UnknownWebSocketId, info) { }
}