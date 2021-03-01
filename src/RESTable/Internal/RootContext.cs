using System;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal.Auth;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Internal
{
    public class RootContext : RESTableContext
    {
        protected override WebSocket CreateWebSocket() => throw new NotImplementedException();
        protected override bool IsWebSocketUpgrade { get; } = false;
        public RootContext(IServiceProvider services) : base(Client.GetInternal(services.GetService<RootAccess>()), services) { }
    }
}