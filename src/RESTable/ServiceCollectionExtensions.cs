﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable;
using RESTable.Auth;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.ProtocolProviders;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.WebSockets;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiKeyAuthenticator(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IRequestAuthenticator, ApiKeyAuthenticator>();
            return serviceCollection;
        }

        public static IServiceCollection AddAllowedCorsOriginsFilter(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IAllowedCorsOriginsFilter, AllowedCorsOriginsFilter>();
            return serviceCollection;
        }

        public static IServiceCollection AddRESTable(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<WebSocketManager>();
            serviceCollection.TryAddSingleton<RESTableConfiguration>();
            serviceCollection.TryAddSingleton<RESTableConfigurator>();
            serviceCollection.TryAddSingleton<TermFactory>(pr => pr.GetRequiredService<TypeCache>().TermFactory);
            serviceCollection.TryAddSingleton<ConditionRedirector>();
            serviceCollection.TryAddSingleton<ResourceCollection>();
            serviceCollection.TryAddSingleton<TerminalResourceProvider>();
            serviceCollection.TryAddSingleton<TermCache>();
            serviceCollection.TryAddSingleton<BinaryResourceProvider>();
            serviceCollection.TryAddSingleton<VirtualResourceProvider>();
            serviceCollection.TryAddSingleton<ResourceFactory>();
            serviceCollection.TryAddSingleton<ContentTypeProviderManager>();
            serviceCollection.TryAddSingleton<ProtocolProviderManager>();
            serviceCollection.TryAddSingleton<TypeCache>();
            serviceCollection.TryAddSingleton<ResourceValidator>();
            serviceCollection.TryAddSingleton(typeof(ConditionCache<>), typeof(ConditionCache<>));
            serviceCollection.TryAddSingleton<ResourceAuthenticator>();
            serviceCollection.TryAddSingleton<IRequestAuthenticator, AllowAllAuthenticator>();
            serviceCollection.TryAddSingleton<IAllowedCorsOriginsFilter, AllCorsOriginsAllowedCors>();
            serviceCollection.TryAddSingleton<RootAccess>();
            serviceCollection.TryAddSingleton<RootClient>();

            serviceCollection.AddSingleton<IEntityResourceProvider, InMemoryEntityResourceProvider>();
            serviceCollection.AddSingleton<IProtocolProvider, DefaultProtocolProvider>();

            return serviceCollection;
        }
    }
}