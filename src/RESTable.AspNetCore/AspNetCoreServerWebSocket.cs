using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using RESTable.Requests;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreServerWebSocket : AspNetCoreWebSocket
    {
        private HttpContext HttpContext { get; }

        public AspNetCoreServerWebSocket(HttpContext httpContext, string webSocketId, RESTableContext context) : base(webSocketId, context)
        {
            HttpContext = httpContext;
            WebSocket = null!;
        }

        protected override async Task ConnectUnderlyingWebSocket(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WebSocket = await HttpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        }
    }
}