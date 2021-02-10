using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RESTable.NetworkProviders;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.AspNetCore
{
    internal class AspNetCoreNetworkProvider : INetworkProvider
    {
        private IRouteBuilder RouteBuilder { get; }

        public AspNetCoreNetworkProvider(IRouteBuilder routeBuilder)
        {
            RouteBuilder = routeBuilder;
        }

        public void AddRoutes(Method[] methods, string rootUri, ushort _)
        {
            var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";

            RouteBuilder.MapVerb("OPTIONS", template, async aspNetCoreContext =>
            {
                var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(rootUri);
                var headers = new Headers(aspNetCoreContext.Request.Headers);
                var client = GetClient(aspNetCoreContext);
                var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
                var options = context.GetOptions(uri, headers);
                await WriteResponse(aspNetCoreContext, options);
            });

            foreach (var method in methods)
            {
                RouteBuilder.MapVerb(method.ToString(), template, async aspNetCoreContext =>
                {
                    var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(rootUri);
                    var headers = new Headers(aspNetCoreContext.Request.Headers);
                    var client = GetClient(aspNetCoreContext);
                    var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
                    if (!context.TryAuthenticate(ref uri, out var notAuthorized, headers))
                    {
                        await WriteResponse(aspNetCoreContext, await notAuthorized.Serialize());
                        return;
                    }
                    var body = aspNetCoreContext.Request.Body;
                    await using var request = context.CreateRequest(uri, method, body, headers);
                    var result = await request.Evaluate();
                    if (result is WebSocketUpgradeSuccessful ws)
                    {
                        await using var webSocket = ws.WebSocket;
                        await webSocket.LifetimeTask;
                    }
                    else
                    {
                        await using var serializedResult = await result.Serialize();
                        await WriteResponse(aspNetCoreContext, serializedResult);
                    }
                });
            }
        }

        public void RemoveRoutes(Method[] methods, string uri, ushort _) { }

        private static async Task WriteResponse(HttpContext context, ISerializedResult serializedResult)
        {
            var result = serializedResult.Result;
            context.Response.StatusCode = (ushort) result.StatusCode;
            result.Headers.ForEach(header => context.Response.Headers[header.Key] = header.Value);
            result.Cookies.ForEach(cookie => context.Response.Headers["Set-Cookie"] = cookie.ToString());
            if (serializedResult.Body != null)
            {
                if (result.Headers.ContentType.HasValue)
                    context.Response.ContentType = result.Headers.ContentType.ToString();
                await using var remote = context.Response.Body;
                await serializedResult.Body.CopyToAsync(remote);
            }
        }

        private static Client GetClient(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress;
            var proxyIp = default(IPAddress);
            var host = context.Request.Host.Host;
            var userAgent = context.Request.Headers["User-Agent"];
            var https = context.Request.IsHttps;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var ip))
            {
                clientIp = IPAddress.Parse(ip.First().Split(':')[0]);
                proxyIp = clientIp;
            }
            return Client.External
            (
                clientIp: clientIp,
                proxyIp: proxyIp,
                userAgent: userAgent,
                host: host,
                https: https,
                cookies: new Cookies(context.Request.Cookies)
            );
        }
    }
}