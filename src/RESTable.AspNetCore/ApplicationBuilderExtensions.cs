using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        private static string RootUri { get; set; }
        private static string Template { get; set; }

        /// <summary>
        /// Adds ASP.NET core routings to work according to the existing RESTable configuration. If RESTable is
        /// not configured prior to this method being called, the default configuration is used. 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRESTableAspNetCore(this IApplicationBuilder builder)
        {
            var configurator = builder.ApplicationServices.GetRequiredService<RESTableConfigurator>();
            if (!configurator.IsConfigured)
                configurator.ConfigureRESTable();
            var config = builder.ApplicationServices.GetRequiredService<RESTableConfiguration>();
            var authenticator = builder.ApplicationServices.GetRequiredService<IRequestAuthenticator>();

            RootUri = config.RootUri;
            Template = RootUri + "/{resource?}/{conditions?}/{metaconditions?}";

            builder.UseRouter(router =>
            {
                router.MapVerb("OPTIONS", Template, HandleOptionsRequest);

                foreach (var method in config.Methods)
                {
                    router.MapVerb(method.ToString(), Template, hc => HandleRequest(method, hc, authenticator));
                }
            });

            return builder;
        }

        private static async Task HandleOptionsRequest(HttpContext aspNetCoreContext)
        {
            var (_, uri) = aspNetCoreContext.Request.Path.Value.TupleSplit(RootUri);
            var headers = new Headers(aspNetCoreContext.Request.Headers);
            var client = GetClient(aspNetCoreContext);
            var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
            var options = context.GetOptions(uri, headers);
            WriteResponse(aspNetCoreContext, options);

            var remote = aspNetCoreContext.Response.Body;
#if NETSTANDARD2_1
            await using (remote)
#else
            using (remote)
#endif
            {
                await using var serializedResult = await options.Serialize(remote).ConfigureAwait(false);
            }
        }

        private static async Task HandleRequest(Method method, HttpContext aspNetCoreContext, IRequestAuthenticator authenticator)
        {
            var (_, uri) = aspNetCoreContext.Request.Path.Value.TupleSplit(RootUri);
            var headers = new Headers(aspNetCoreContext.Request.Headers);
            var client = GetClient(aspNetCoreContext);
            var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
            if (!authenticator.TryAuthenticate(context, ref uri, headers, out var notAuthorized))
            {
                WriteResponse(aspNetCoreContext, notAuthorized);
                return;
            }
            var body = aspNetCoreContext.Request.Body;
            var request = context.CreateRequest(method, uri, body, headers);
            await using var result = await request.GetResult().ConfigureAwait(false);
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
                    var remote = aspNetCoreContext.Response.Body;

#if NETSTANDARD2_1
                    await using (remote)
#else
                    using (remote)
#endif
                    {
                        await using var serializedResult = await result.Serialize(remote).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }

        private static void WriteResponse(HttpContext context, IResult result)
        {
            context.Response.StatusCode = (ushort) result.StatusCode;
            foreach (var (key, value) in result.Headers)
                context.Response.Headers[key] = value;
            foreach (var cookie in result.Cookies)
                context.Response.Headers["Set-Cookie"] = cookie.ToString();
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