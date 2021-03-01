using System;
using System.Net;
using RESTable.Requests;
using RESTable.WebSockets;

namespace RESTable.Tests
{
    public class MockContext : RESTableContext
    {
        private static Client MockClient => Client.External
        (
            clientIp: IPAddress.Parse("151.10.10.5"),
            proxyIp: null,
            userAgent: "Some User-Agent!",
            host: "the host header",
            https: true,
            cookies: new Cookies()
        );

        public MockContext(IServiceProvider serviceProvider) : base(MockClient, serviceProvider) { }

        protected override bool IsWebSocketUpgrade => false;

        protected override WebSocket CreateWebSocket()
        {
            throw new NotImplementedException();
        }
    }
}