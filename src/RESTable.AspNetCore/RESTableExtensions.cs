using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.NetworkProviders;
using RESTable.ProtocolProviders;
using RESTable.Resources;

namespace RESTable.AspNetCore
{
    public static class RESTableExtensions
    {
        public static IApplicationBuilder UseRESTable
        (
            this IApplicationBuilder builder,
            string uri = "/rest",
            bool requireApiKey = false,
            bool allowAllOrigins = true,
            string configFilePath = null,
            bool prettyPrint = true,
            LineEndings lineEndings = LineEndings.Windows
        )
        {
            builder.UseWebSockets();
            builder.UseRouter(router => RESTableConfig.Init
            (
                uri: uri,
                requireApiKey: requireApiKey,
                allowAllOrigins: allowAllOrigins,
                configFilePath: configFilePath,
                prettyPrint: prettyPrint,
                nrOfErrorsToKeep: 30,
                lineEndings: lineEndings,
                entityResourceProviders: builder.ApplicationServices.GetServices<IEntityResourceProvider>(),
                protocolProviders: builder.ApplicationServices.GetServices<IProtocolProvider>(),
                contentTypeProviders: builder.ApplicationServices.GetServices<IContentTypeProvider>(),
                networkProviders: new INetworkProvider[] {new AspNetCoreNetworkProvider(router)},
                entityTypeContractResolvers: builder.ApplicationServices.GetServices<IEntityTypeContractResolver>()
            ));
            Application.Services = builder.ApplicationServices;
            return builder;
        }
    }
}