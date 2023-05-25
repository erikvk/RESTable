using System;
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
            var handler = ActivatorUtilities.CreateInstance<HttpRequestHandler>(builder.ApplicationServices);
            router.MapVerb("OPTIONS", template, context => handler.HandleOptionsRequest(rootUri, context));
            foreach (var method in EnumMember<Method>.Values)
            {
                router.MapVerb(method.ToString(), template, async hc =>
                {
                    try
                    {
                        await handler.HandleRequest(rootUri, method, hc).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (hc.RequestAborted.IsCancellationRequested)
                    {
                        // Ignore
                    }
                });
            }
        });

        return builder;
    }
}
