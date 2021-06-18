using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.WebSockets;

namespace RESTable.AspNetCore
{
    public class ClientWebSocketBuilder : IProtocolHolder
    {
        private Uri? Uri { get; set; }
        private string WebSocketId { get; set; }
        public RESTableContext Context { get; }
        public Headers Headers { get; }
        public string? HeadersStringCache { get; set; }
        public bool ExcludeHeaders => false;
        public string ProtocolIdentifier { get; }
        public CachedProtocolProvider CachedProtocolProvider { get; }
        private Terminal? Terminal { get; set; }
        private Func<IWebSocket, string, Task>? TextInputHandler { get; set; }
        private Func<IWebSocket, Stream, Task>? BinaryInputHandler { get; set; }
        private Func<IWebSocket, Task>? OpenHandler { get; set; }
        private Func<IWebSocket, ValueTask>? DisposeHandler { get; set; }

        public ClientWebSocketBuilder(RESTableContext context)
        {
            WebSocketId = Guid.NewGuid().ToString("N");
            Context = context;
            Headers = new Headers();
            var defaultProtocolProvider = context.GetRequiredService<ProtocolProviderManager>().DefaultProtocolProvider;
            ProtocolIdentifier = defaultProtocolProvider.ProtocolProvider.ProtocolIdentifier;
            CachedProtocolProvider = defaultProtocolProvider;
        }

        public ClientWebSocketBuilder WithUri(Uri uri)
        {
            Uri = uri;
            return this;
        }

        public ClientWebSocketBuilder WithUri(string uriString)
        {
            var uri = new Uri(uriString);
            return WithUri(uri);
        }


        public ClientWebSocketBuilder WithWebSocketId(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return this;
            WebSocketId = id;
            return this;
        }

        public ClientWebSocketBuilder WithHeaders(Action<Headers> headersAction)
        {
            headersAction(Headers);
            return this;
        }

        public ClientWebSocketBuilder WithTerminal(Terminal terminal)
        {
            Terminal = terminal;
            return this;
        }

        public ClientWebSocketBuilder HandleTextInput(Func<IWebSocket, string, Task> handler)
        {
            TextInputHandler = handler;
            return this;
        }

        public ClientWebSocketBuilder HandleBinaryInput(Func<IWebSocket, Stream, Task> handler)
        {
            BinaryInputHandler = handler;
            return this;
        }

        public ClientWebSocketBuilder OnOpen(Func<IWebSocket, Task> handler)
        {
            OpenHandler = handler;
            return this;
        }

        public ClientWebSocketBuilder OnDispose(Func<IWebSocket, ValueTask> handler)
        {
            DisposeHandler = handler;
            return this;
        }

        public async Task Connect()
        {
            if (Uri is null)
                throw new InvalidOperationException("Missing or invalid Uri");

            var clientWebSocket = new ClientWebSocket();
            foreach (var (key, value) in Headers)
            {
                clientWebSocket.Options.SetRequestHeader(key, value);
            }
            var aspNetCoreWebSocket = new AspNetCoreClientWebSocket
            (
                webSocket: clientWebSocket,
                remoteUri: Uri!,
                webSocketId: WebSocketId,
                context: Context
            );
            await aspNetCoreWebSocket.OpenAndAttachToTerminal
            (
                protocolHolder: this,
                terminal: Terminal ?? new CustomTerminal(this)
            ).ConfigureAwait(false);
        }

        private sealed class CustomTerminal : Terminal, IAsyncDisposable
        {
            private ClientWebSocketBuilder ClientWebSocketBuilder { get; }

            public CustomTerminal(ClientWebSocketBuilder clientWebSocketBuilder) => ClientWebSocketBuilder = clientWebSocketBuilder;

            protected override async Task Open()
            {
                if (ClientWebSocketBuilder.OpenHandler is { } handler)
                {
                    await handler(WebSocket).ConfigureAwait(false);
                }
            }

            public async ValueTask DisposeAsync()
            {
                if (ClientWebSocketBuilder.DisposeHandler is { } handler)
                {
                    await handler(WebSocket).ConfigureAwait(false);
                }
            }

            public override async Task HandleTextInput(string input) => await ClientWebSocketBuilder.TextInputHandler!(WebSocket, input).ConfigureAwait(false);
            public override async Task HandleBinaryInput(Stream input) => await ClientWebSocketBuilder.BinaryInputHandler!(WebSocket, input).ConfigureAwait(false);
            protected override bool SupportsTextInput => ClientWebSocketBuilder.TextInputHandler is not null;
            protected override bool SupportsBinaryInput => ClientWebSocketBuilder.BinaryInputHandler is not null;
        }
    }
}