using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;

namespace RESTable.AspNetCore;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     Adds ASP.NET core routings to work according to the existing RESTable configuration.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="rootUri">The root URI to use for the RESTable REST API</param>
    /// <returns></returns>
    public static IApplicationBuilder UseRESTableAspNetCore(this IApplicationBuilder builder, string rootUri = "/api")
    {
        var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";
        builder.UseRouter(router =>
        {
            HttpRequestHandler? handler = null;

            router.MapVerb("OPTIONS", template, context =>
            {
                handler ??= ActivatorUtilities.CreateInstance<HttpRequestHandler>(builder.ApplicationServices);
                return handler.HandleOptionsRequest(rootUri, context);
            });

            foreach (var method in EnumMember<Method>.Values)
            {
                router.MapVerb(method.ToString(), template, hc =>
                {
                    try
                    {
                        handler ??= ActivatorUtilities.CreateInstance<HttpRequestHandler>(builder.ApplicationServices);
                        return handler.HandleRequest(rootUri, method, hc);
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
}