using System;
using Microsoft.AspNetCore.Http;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreRESTableContext : RESTableContext
    {
        private HttpContext HttpContext { get; }

        public AspNetCoreRESTableContext(Requests.Client client, HttpContext httpContext) : base(client, httpContext.RequestServices)
        {
            HttpContext = httpContext;
        }

        protected override bool IsWebSocketUpgrade => HttpContext.WebSockets.IsWebSocketRequest;

        protected override WebSocket CreateServerWebSocket()
        {
            return new AspNetCoreServerWebSocket(HttpContext, Guid.NewGuid().ToString("N"), this);
        }
    }
}