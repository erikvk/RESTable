using System.Linq;
using System.Net;
using System.Threading;
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
            var authenticator = builder.ApplicationServices.GetService<IRequestAuthenticator>() ??
                                builder.ApplicationServices.GetRequiredService<IAllowAllAuthenticator>();

            var rootUri = config.RootUri;
            var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";

            builder.UseRouter(router =>
            {
                router.MapVerb("OPTIONS", template, context => HandleOptionsRequest(rootUri, context));

                foreach (var method in config.Methods)
                {
                    router.MapVerb(method.ToString(), template, hc => HandleRequest(rootUri, method, hc, authenticator, hc.RequestAborted));
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
            WriteResponse(aspNetCoreContext, options);

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
                WriteResponse(aspNetCoreContext, error);
                return;
            }
            var client = GetClient(aspNetCoreContext, accessRights);
            var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);

            var body = aspNetCoreContext.Request.Body;
            var request = context.CreateRequest(method, uri, body, headers);
            await using var result = await request.GetResult(cancellationToken);
            switch (result)
            {
                case WebSocketTransferSuccess:
                    return;
                case WebSocketUpgradeSuccessful ws:
                {
                    await using var webSocket = ws.WebSocket;
                    await webSocket.LifetimeTask;
                    break;
                }
                default:
                {
                    WriteResponse(aspNetCoreContext, result);
                    var remote = aspNetCoreContext.Response.Body;

#if NETSTANDARD2_0
                    using (remote)
#else
                    await using (remote)
#endif
                    {
                        await using var serializedResult = await result.Serialize(remote, cancellationToken: cancellationToken);
                    }
                    break;
                }
            }
        }

        private static void WriteResponse(HttpContext context, IResult result)
        {
            context.Response.StatusCode = (ushort) result.StatusCode;
            foreach (var (key, value) in result.Headers)
            {
                if (value is null) continue;
                // Kestrel doesn't like line breaks in headers
                context.Response.Headers[key] = value.Replace(System.Environment.NewLine, null);
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
                cookies: new Cookies(context.Request.Cookies),
                accessRights: accessRights
            );
        }
    }
}