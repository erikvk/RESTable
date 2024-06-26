﻿using RESTable.Requests;

namespace RESTable.WebSockets;

internal sealed class AppProfile
{
    internal AppProfile(WebSocket webSocket)
    {
        Host = webSocket.Context.Client.Host;
        WebSocketId = webSocket.Context.TraceId;
        IsEncrypted = webSocket.Context.Client.Https;
        ClientIP = webSocket.Context.Client.ClientIp;
        ConnectedAt = webSocket.OpenedAt.ToString("yyyy-MM-dd HH:mm:ss");
        CurrentTerminal = webSocket.TerminalResource?.Name ?? "none";
        CustomHeaders = new Headers();
        foreach (var (key, value) in webSocket.Headers.GetCustom())
            CustomHeaders.Add(key, value);
    }

    public string? Host { get; }
    public string WebSocketId { get; }
    public bool IsEncrypted { get; }
    public string? ClientIP { get; }
    public string ConnectedAt { get; }
    public string CurrentTerminal { get; }
    public Headers CustomHeaders { get; set; }
}
