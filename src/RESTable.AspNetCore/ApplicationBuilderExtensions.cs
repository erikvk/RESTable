using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        private static string RootUri { get; set; }
        private static string Template { get; set; }

        public static IApplicationBuilder UseRESTableAspNetCore(this IApplicationBuilder builder)
        {
            var config = builder.ApplicationServices.GetService<RESTableConfig>();

            RootUri = config.RootUri;
            Template = RootUri + "/{resource?}/{conditions?}/{metaconditions?}";

            builder.UseRouter(router =>
            {
                router.MapVerb("OPTIONS", Template, HandleOptionsRequest);

                foreach (var method in config.Methods)
                {
                    router.MapVerb(method.ToString(), Template, context => HandleRequest(method, context));
                }
            });

            return builder;
        }

        private static async Task HandleOptionsRequest(HttpContext aspNetCoreContext)
        {
            var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(RootUri);
            var headers = new Headers(aspNetCoreContext.Request.Headers);
            var client = GetClient(aspNetCoreContext);
            var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
            var options = context.GetOptions(uri, headers);
            WriteResponse(aspNetCoreContext, options);
            await using var remote = aspNetCoreContext.Response.Body;
            await using var serializedResult = await options.Serialize(remote).ConfigureAwait(false);
        }

        private static async Task HandleRequest(Method method, HttpContext aspNetCoreContext)
        {
            var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(RootUri);
            var headers = new Headers(aspNetCoreContext.Request.Headers);
            var client = GetClient(aspNetCoreContext);
            var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
            if (!context.TryAuthenticate(ref uri, out var notAuthorized, headers))
            {
                WriteResponse(aspNetCoreContext, notAuthorized);
                return;
            }
            var body = aspNetCoreContext.Request.Body;
            await using var request = context.CreateRequest(method, uri, body, headers);
            var result = await request.Evaluate().ConfigureAwait(false);
            switch (result)
            {
                case WebSocketTransferSuccess:
                    return;
                case WebSocketUpgradeSuccessful ws:
                {
                    await using var webSocket = ws.WebSocket;
                    await webSocket.LifetimeTask.ConfigureAwait(false);
                    break;
                }
                default:
                {
                    WriteResponse(aspNetCoreContext, result);
                    await using var remote = aspNetCoreContext.Response.Body;
                    await using var serializedResult = await result.Serialize(remote).ConfigureAwait(false);
                    break;
                }
            }
        }

        private static void WriteResponse(HttpContext context, IResult result)
        {
            context.Response.StatusCode = (ushort) result.StatusCode;
            result.Headers.ForEach(header => context.Response.Headers[header.Key] = header.Value);
            result.Cookies.ForEach(cookie => context.Response.Headers["Set-Cookie"] = cookie.ToString());
            if (result.Headers.ContentType.HasValue)
                context.Response.ContentType = result.Headers.ContentType.ToString();
        }

        private static Client GetClient(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress;
            var proxyIp = default(IPAddress);
            var host = context.Request.Host.Host;
            var userAgent = context.Request.Headers["User-Agent"];
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
                https: context.Request.IsHttps,
                cookies: new Cookies(context.Request.Cookies)
            );
        }
    }
}