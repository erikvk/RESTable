using System;
using Microsoft.AspNetCore.Http;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.AspNetCore;

internal class AspNetCoreRESTableContext : RESTableContext
{
    public AspNetCoreRESTableContext(Client client, HttpContext httpContext) : base(client, httpContext.RequestServices)
    {
        HttpContext = httpContext;
    }

    private HttpContext HttpContext { get; }

    protected override bool IsWebSocketUpgrade => HttpContext.WebSockets.IsWebSocketRequest;

    protected override WebSocket CreateServerWebSocket()
    {
        return new AspNetCoreServerWebSocket(HttpContext, Guid.NewGuid().ToString("N"), this);
    }
}