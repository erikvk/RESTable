﻿using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.WebSockets;

namespace RESTable.AspNetCore;

public class ClientWebSocketBuilder : IProtocolHolder
{
    public ClientWebSocketBuilder(RESTableContext context)
    {
        WebSocketId = Guid.NewGuid().ToString("N");
        Context = context;
        Headers = new Headers();
        var defaultProtocolProvider = context.GetRequiredService<ProtocolProviderManager>().DefaultProtocolProvider;
        ProtocolIdentifier = defaultProtocolProvider.ProtocolProvider.ProtocolIdentifier;
        CachedProtocolProvider = defaultProtocolProvider;
        var jsonProvider = context.GetRequiredService<IJsonProvider>();
        InputContentTypeProvider = jsonProvider;
        OutputContentTypeProvider = jsonProvider;
    }

    private Uri? Uri { get; set; }
    private string WebSocketId { get; set; }
    private bool CertificateErrorsIgnored { get; set; }
    private Terminal? Terminal { get; set; }
    private Func<IWebSocket, string, CancellationToken, Task>? TextInputHandler { get; set; }
    private Func<IWebSocket, Stream, CancellationToken, Task>? BinaryInputHandler { get; set; }
    private Func<IWebSocket, CancellationToken, Task>? OpenHandler { get; set; }
    private Func<IWebSocket, ValueTask>? DisposeHandler { get; set; }
    public RESTableContext Context { get; }
    public Headers Headers { get; }
    public string? HeadersStringCache { get; set; }
    public bool ExcludeHeaders => false;
    public string ProtocolIdentifier { get; }
    public CachedProtocolProvider CachedProtocolProvider { get; }

    public IContentTypeProvider InputContentTypeProvider { get; }
    public IContentTypeProvider OutputContentTypeProvider { get; }

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

    public ClientWebSocketBuilder IgnoreCertificateErrors(bool ignore)
    {
        CertificateErrorsIgnored = ignore;
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

    public ClientWebSocketBuilder HandleTextInput(Func<IWebSocket, string, CancellationToken, Task> handler)
    {
        TextInputHandler = handler;
        return this;
    }

    public ClientWebSocketBuilder HandleBinaryInput(Func<IWebSocket, Stream, CancellationToken, Task> handler)
    {
        BinaryInputHandler = handler;
        return this;
    }

    public ClientWebSocketBuilder OnOpen(Func<IWebSocket, CancellationToken, Task> handler)
    {
        OpenHandler = handler;
        return this;
    }

    public ClientWebSocketBuilder OnDispose(Func<IWebSocket, ValueTask> handler)
    {
        DisposeHandler = handler;
        return this;
    }

    public async Task<IWebSocket> Connect(CancellationToken cancellationToken)
    {
        if (Uri is null)
            throw new InvalidOperationException("Missing or invalid Uri");

        var clientWebSocket = new ClientWebSocket();
        foreach (var (key, value) in Headers)
            clientWebSocket.Options.SetRequestHeader(key, value);
        // Auth headers are masked in enumeration
        if (Headers.Authorization is string authHeader)
            clientWebSocket.Options.SetRequestHeader("Authorization", authHeader);

        var aspNetCoreWebSocket = new AspNetCoreClientWebSocket
        (
            clientWebSocket,
            Uri!,
            WebSocketId,
            Context,
            CertificateErrorsIgnored
        );

        await aspNetCoreWebSocket.OpenAndAttachClientSocketToTerminal
        (
            this,
            Terminal ?? new CustomTerminal(this),
            cancellationToken
        ).ConfigureAwait(false);

        return aspNetCoreWebSocket;
    }

    private sealed class CustomTerminal : Terminal, IAsyncDisposable
    {
        public CustomTerminal(ClientWebSocketBuilder clientWebSocketBuilder) : base
        (
            clientWebSocketBuilder.TextInputHandler is not null,
            clientWebSocketBuilder.BinaryInputHandler is not null
        )
        {
            ClientWebSocketBuilder = clientWebSocketBuilder;
        }

        private ClientWebSocketBuilder ClientWebSocketBuilder { get; }

        public async ValueTask DisposeAsync()
        {
            if (ClientWebSocketBuilder.DisposeHandler is { } handler)
                await handler(WebSocket).ConfigureAwait(false);
        }

        protected override async Task Open(CancellationToken cancellationToken)
        {
            if (ClientWebSocketBuilder.OpenHandler is { } handler)
                await handler(WebSocket, cancellationToken).ConfigureAwait(false);
        }

        public override async Task HandleTextInput(string input, CancellationToken cancellationToken)
        {
            if (ClientWebSocketBuilder.TextInputHandler is not null)
            {
                await ClientWebSocketBuilder.TextInputHandler(WebSocket, input, cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task HandleBinaryInput(Stream input, CancellationToken cancellationToken)
        {
            if (ClientWebSocketBuilder.BinaryInputHandler is not null)
            {
                await ClientWebSocketBuilder.BinaryInputHandler(WebSocket, input, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
