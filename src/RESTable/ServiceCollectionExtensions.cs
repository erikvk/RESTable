using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable;
using RESTable.Internal;
using RESTable.Internal.Auth;
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
        public static IServiceCollection AddRESTable(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<WebSocketController>();
            serviceCollection.TryAddSingleton<RESTableConfiguration>();
            serviceCollection.TryAddSingleton<RESTableConfigurator>();
            serviceCollection.TryAddSingleton<TermFactory>(pr => pr.GetService<TypeCache>().TermFactory);
            serviceCollection.TryAddSingleton<ConditionRedirector>();
            serviceCollection.TryAddSingleton<ResourceCollection>();
            serviceCollection.TryAddSingleton<TerminalResourceProvider>();
            serviceCollection.TryAddSingleton<TermCache>();
            serviceCollection.TryAddSingleton<BinaryResourceProvider>();
            serviceCollection.TryAddSingleton<VirtualResourceProvider>();
            serviceCollection.TryAddSingleton<EntityTypeResolverController>();
            serviceCollection.TryAddSingleton<ResourceFactory>();
            serviceCollection.TryAddSingleton<ContentTypeController>();
            serviceCollection.TryAddSingleton<ProtocolController>();
            serviceCollection.TryAddSingleton<TypeCache>();
            serviceCollection.TryAddSingleton<ResourceValidator>();
            serviceCollection.TryAddSingleton(typeof(ConditionCache<>), typeof(ConditionCache<>));
            serviceCollection.TryAddSingleton<Authenticator>();
            serviceCollection.TryAddSingleton<RootAccess>();

            serviceCollection.AddSingleton<IEntityResourceProvider, InMemoryEntityResourceProvider>();
            serviceCollection.AddSingleton<IProtocolProvider, DefaultProtocolProvider>();

            return serviceCollection;
        }
    }
}