using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;
using RESTable.WebSockets;

namespace RESTable.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds ASP.NET core routings to work according to the existing RESTable configuration. 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="rootUri">The root URI to use for the RESTable REST API</param>
        /// <returns></returns>
        public static IApplicationBuilder UseRESTableAspNetCore(this IApplicationBuilder builder, string rootUri = "/restable")
        {
            var services = builder.ApplicationServices.GetRequiredService<RootContext>();
            var authenticator = services.GetService<IRequestAuthenticator>() ?? services.GetRequiredService<IAllowAllAuthenticator>();

            var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";

            builder.UseRouter(router =>
            {
                router.MapVerb("OPTIONS", template, context => HandleOptionsRequest(rootUri, context));

                foreach (var method in EnumMember<Method>.Values)
                {
                    router.MapVerb(method.ToString(), template, hc =>
                    {
                        try
                        {
                            return HandleRequest(rootUri, method, hc, authenticator, hc.RequestAborted);
                        }
                        catch (OperationCanceledException)
                        {
                            return Task.FromCanceled(hc.RequestAborted);
                        }
                    });
                }
            });

            return builder;
        }

        private static async Task HandleOptionsRequest(string rootUri, HttpContext aspNetCoreContext)
        {
            var (_, uri) = aspNetCoreContext.Request.Path.Value.TupleSplit(rootUri);
            var headers = new Headers(aspNetCoreContext.Request.Headers);
            var client = GetClient(aspNetCoreContext, new NoAccess());
            var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
            var options = context.GetOptions(uri, headers);
            WriteResponse(options, aspNetCoreContext);

            var remote = aspNetCoreContext.Response.Body;
#if NETSTANDARD2_0
            using (remote)
#else
            await using (remote)
#endif
            {
                await using var serializedResult = await options.Serialize(remote);
            }
        }

        private static async Task HandleRequest(string rootUri, Method method, HttpContext aspNetCoreContext, IRequestAuthenticator authenticator,
            CancellationToken cancellationToken)
        {
            var (_, uri) = aspNetCoreContext.Request.Path.Value.TupleSplit(rootUri);
            var headers = new Headers(aspNetCoreContext.Request.Headers);
            if (!authenticator.TryAuthenticate(ref uri, headers, out var accessRights))
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
                case WebSocketTransferSuccess: return;
                case WebSocketUpgradeFailed wuf:
                {
                    // An error occured during the upgrade process
                    var webSocket = wuf.WebSocket;
                    if (webSocket.Status == WebSocketStatus.Open)
                    {
                        // We're already open. Set the close description and close the websocket.
                        await using (webSocket.ConfigureAwait(false))
                        {
                            webSocket.CloseDescription = await wuf.Error.GetLogMessage().ConfigureAwait(false);
                            break;
                        }
                    }
                    // We're not open yet. Respond with a regular HTTP error response
                    WriteResponse(wuf.Error, aspNetCoreContext);
                    await WriteResponseBody(wuf.Error, aspNetCoreContext, cancellationToken).ConfigureAwait(false);
                    break;
                }
                case WebSocketUpgradeSuccessful {WebSocket: var webSocket}:
                {
                    await using (webSocket.ConfigureAwait(false))
                    {
                        await webSocket.LifetimeTask;
                        break;
                    }
                }
                default:
                {
                    WriteResponse(result, aspNetCoreContext);
                    await WriteResponseBody(result, aspNetCoreContext, cancellationToken).ConfigureAwait(false);
                    break;
                }
            }
        }

        private static async Task WriteResponseBody(IResult result, HttpContext aspNetCoreContext, CancellationToken cancellationToken)
        {
            var remote = aspNetCoreContext.Response.Body;
#if NETSTANDARD2_0
            using (remote)
#else
            await using (remote)
#endif
            {
                await using var serializedResult = await result.Serialize(remote, cancellationToken: cancellationToken);
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

        private static Client GetClient(HttpContext context, AccessRights accessRights)
        {
            var clientIp = context.Connection.RemoteIpAddress;
            var proxyIp = default(IPAddress);
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ip))
            {
                clientIp = IPAddress.Parse(ip.First().Split(':')[0]);
                proxyIp = clientIp;
            }
            return Client.External
            (
                clientIp: clientIp,
                proxyIp: proxyIp,
                userAgent: context.Request.Headers["User-Agent"],
                host: context.Request.Host.Value,
                https: context.Request.IsHttps,
                cookies: new Cookies(context.Request.Cookies),
                accessRights: accessRights
            );
        }
    }
}