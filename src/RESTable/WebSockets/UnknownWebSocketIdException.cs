﻿namespace RESTable.WebSockets;

public class UnknownWebSocketIdException : RESTableException
{
    public UnknownWebSocketIdException(string info) : base(ErrorCodes.UnknownWebSocketId, info) { }
}
