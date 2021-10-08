using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Requests;

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
            var template = rootUri + "/{resource?}/{conditions?}/{metaconditions?}";
            builder.UseRouter(router =>
            {
                // Make sure RESTable is initialized
                var _ = builder.ApplicationServices.GetRequiredService<RootContext>();
                var handler = ActivatorUtilities.CreateInstance<HttpRequestHandler>(builder.ApplicationServices);

                router.MapVerb("OPTIONS", template, context => handler.HandleOptionsRequest(rootUri, context));

                foreach (var method in EnumMember<Method>.Values)
                {
                    router.MapVerb(method.ToString(), template, hc =>
                    {
                        try
                        {
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
}