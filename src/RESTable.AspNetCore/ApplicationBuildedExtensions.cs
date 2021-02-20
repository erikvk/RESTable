﻿using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RESTable.Admin;
using RESTable.Requests;
using RESTable.Results;
using RESTable.Linq;

namespace RESTable.AspNetCore
{
    public static class ApplicationBuildedExtensions
    {
        public static IApplicationBuilder UseRESTableAspNetCore(this IApplicationBuilder builder)
        {
            if (!RESTableConfig.Initialized)
                throw new InvalidOperationException($"RESTable not initialized prior to call to {nameof(UseRESTableAspNetCore)}");
            var rootUri = Settings._Uri;
            var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";

            builder.UseRouter(router =>
            {
                router.MapVerb("OPTIONS", template, async aspNetCoreContext =>
                {
                    var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(rootUri);
                    var headers = new Headers(aspNetCoreContext.Request.Headers);
                    var client = GetClient(aspNetCoreContext);
                    var context = new AspNetCoreRESTableContext(client, aspNetCoreContext);
                    var options = context.GetOptions(uri, headers);
                    WriteResponse(aspNetCoreContext, options);
                    await using var remote = aspNetCoreContext.Response.Body;
                    await using var serializedResult = await options.Serialize(remote).ConfigureAwait(false);
                });

                foreach (var method in RESTableConfig.Methods)
                {
                    router.MapVerb(method.ToString(), template, async aspNetCoreContext =>
                    {
                        var (_, uri) = aspNetCoreContext.Request.Path.Value.TSplit(rootUri);
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
                        if (result is WebSocketUpgradeSuccessful ws)
                        {
                            await using var webSocket = ws.WebSocket;
                            await webSocket.LifetimeTask.ConfigureAwait(false);
                        }
                        else
                        {
                            WriteResponse(aspNetCoreContext, result);
                            await using var remote = aspNetCoreContext.Response.Body;
                            await using var serializedResult = await result.Serialize(remote).ConfigureAwait(false);
                            serializedResult.Result.ThrowIfError();
                        }
                    });
                }
            });

            return builder;
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