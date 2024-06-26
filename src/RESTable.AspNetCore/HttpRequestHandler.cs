using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RESTable.Auth;
using RESTable.Requests;
using RESTable.Results;
using RESTable.WebSockets;

namespace RESTable.AspNetCore;

public class HttpRequestHandler
{
    private ILogger<HttpRequestHandler> Logger { get; }
    private IRequestAuthenticator Authenticator { get; }

    public HttpRequestHandler(ILogger<HttpRequestHandler> logger, IRequestAuthenticator authenticator)
    {
        Logger = logger;
        Authenticator = authenticator;
    }

    public async Task HandleOptionsRequest(string rootUri, HttpContext aspNetCoreContext)
    {
        var (_, uri) = aspNetCoreContext.Request.Path.Value.TupleSplit(rootUri);
        var headers = new Headers(aspNetCoreContext.Request.Headers);
        var client = GetClient(aspNetCoreContext, new NoAccess());
        var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
        var result = context.GetOptions(uri, headers);
        WriteResponse(result, aspNetCoreContext);
        var remote = aspNetCoreContext.Response.Body;
        await using (remote.ConfigureAwait(false))
        {
            var serializedResult = await result.Serialize(remote, aspNetCoreContext.RequestAborted).ConfigureAwait(false);
            await using (serializedResult.ConfigureAwait(false)) { }
        }
    }

    public async Task HandleRequest(string rootUri, Method method, HttpContext aspNetCoreContext)
    {
        var cancellationToken = aspNetCoreContext.RequestAborted;
        cancellationToken.ThrowIfCancellationRequested();
        var (_, uri) = aspNetCoreContext.Request.Path.Value.TupleSplit(rootUri);
        if (aspNetCoreContext.Request.QueryString.HasValue)
        {
            uri += aspNetCoreContext.Request.QueryString.Value;
        }
        var headers = new Headers(aspNetCoreContext.Request.Headers);
        if (!Authenticator.TryAuthenticate(ref uri, headers, out var accessRights))
        {
            var error = new Unauthorized();
            if (headers.Metadata == "full")
                error.Headers.Metadata = error.Metadata;
            WriteResponse(error, aspNetCoreContext);
            return;
        }

        var client = GetClient(aspNetCoreContext, accessRights);
        var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);

        var body = aspNetCoreContext.Request.Body;
        var request = context.CreateRequest(method, uri, headers, body);
        await using var result = await request.GetResult(cancellationToken);
        switch (result)
        {
            case WebSocketTransferSuccess wts:
            {
                if (wts.Result is Error error) Logger.LogError("{Message}", await error.GetLogMessage());
                return;
            }
            case WebSocketUpgradeFailed wuf:
            {
                // An error occured during the upgrade process
                var webSocket = wuf.WebSocket;
                if (webSocket is not null)
                {
                    if (webSocket.Status == WebSocketStatus.Open)
                    {
                        Logger.LogError("A WebSocket upgrade request has failed after the WebSocket '{WebSocketId}' was opened. RESTable is sending the error message " +
                                        "as status description, and starting the close handshake. {Message}", webSocket.Context.TraceId, await wuf.Error.GetLogMessage());
                        // We're already open. Set the close description and close the websocket.
                        await using (webSocket.ConfigureAwait(false))
                        {
                            webSocket.CloseDescription = await wuf.Error.GetLogMessage().ConfigureAwait(false);
                            break;
                        }
                    }
                    Logger.LogError("A WebSocket upgrade request has failed before the WebSocket '{WebSocketId}' was opened. RESTable is sending the error message " +
                                    "as HTTP response to the initial WebSocket upgrade request. {Message}", webSocket.Context.TraceId, await wuf.Error.GetLogMessage());
                    await webSocket.DisposeAsync().ConfigureAwait(false);
                }
                // We're not open yet. Respond with a regular HTTP error response
                WriteResponse(wuf.Error, aspNetCoreContext);
                await WriteResponseBody(wuf.Error, aspNetCoreContext, cancellationToken).ConfigureAwait(false);
                break;
            }
            case WebSocketUpgradeSuccessful { WebSocket: WebSocket webSocket }:
            {
                await using (webSocket.ConfigureAwait(false))
                {
                    if (webSocket.LifetimeTask is not null)
                    {
                        await webSocket.LifetimeTask.ConfigureAwait(false);
                    }
                }
                break;
            }
            default:
            {
                if (result is Error error) Logger.LogError("{Message}", await error.GetLogMessage());
                WriteResponse(result, aspNetCoreContext);
                await WriteResponseBody(result, aspNetCoreContext, cancellationToken).ConfigureAwait(false);
                break;
            }
        }
    }

    private static void WriteResponse(IResult result, HttpContext context)
    {
        context.Response.StatusCode = (ushort) result.StatusCode;
        foreach (var (key, value) in result.Headers)
        {
            if (value is null) continue;
            // Kestrel doesn't like line breaks in headers
            context.Response.Headers[key] = value.Replace(Environment.NewLine, null);
        }
        foreach (var cookie in result.Cookies)
            context.Response.Headers["Set-Cookie"] = cookie.ToString();
        if (result.Headers.ContentType.HasValue)
            context.Response.ContentType = result.Headers.ContentType.ToString();
    }

    private static async Task WriteResponseBody(IResult result, HttpContext aspNetCoreContext, CancellationToken cancellationToken)
    {
        var remote = aspNetCoreContext.Response.Body;
        await using (remote.ConfigureAwait(false))
        {
            var serializedResult = await result.Serialize(remote, cancellationToken).ConfigureAwait(false);
            await using (serializedResult.ConfigureAwait(false)) { }
        }
    }

    private static Client GetClient(HttpContext context, AccessRights accessRights)
    {
        var clientIp = context.Connection.RemoteIpAddress;
        var proxyIp = default(IPAddress);
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ip))
        {
            clientIp = IPAddress.Parse(ip.First()?.Split(':')[0] ?? string.Empty);
            proxyIp = clientIp;
        }
        var isForwardedHttps = context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProto) && forwardedProto == "https";
        return Client.External
        (
            clientIp,
            proxyIp,
            context.Request.Headers["User-Agent"],
            context.Request.Host.Value,
            context.Request.IsHttps || isForwardedHttps,
            new Cookies(context.Request.Cookies),
            accessRights
        );
    }
}
