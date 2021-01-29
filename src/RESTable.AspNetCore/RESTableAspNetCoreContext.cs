using System;
using Microsoft.AspNetCore.Http;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.AspNetCore
{
    internal class RESTableAspNetCoreContext : RESTableContext
    {
        private HttpContext HttpContext { get; }

        public RESTableAspNetCoreContext(Client client, HttpContext httpContext) : base(client)
        {
            HttpContext = httpContext;
        }
        protected override bool IsWebSocketUpgrade => HttpContext.WebSockets.IsWebSocketRequest;

        protected override WebSocket CreateWebSocket()
        {
            return new AspNetCoreWebSocket(HttpContext, Guid.NewGuid().ToString("N"), Client);
        }
    }
}