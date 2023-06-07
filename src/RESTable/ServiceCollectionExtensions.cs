using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RESTable;
using RESTable.Auth;
using RESTable.DefaultProtocol;
using RESTable.Internal;
using RESTable.Json;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.WebSockets;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiKeys(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddOptions<ApiKeys>()
            .Bind(configuration.GetSection(ApiKeys.ConfigSection));
        serviceCollection.TryAddSingleton<IApiKeyAuthenticator, ApiKeyAuthenticator>();
        serviceCollection.AddSingleton<IRequestAuthenticator>(pr => pr.GetRequiredService<IApiKeyAuthenticator>());
        serviceCollection.AddStartupActivator<IApiKeyAuthenticator>();
        return serviceCollection;
    }

    public static IServiceCollection AddApiKeys(this IServiceCollection serviceCollection, Action<OptionsBuilder<ApiKeys>>? builderAction = null)
    {
        var builder = serviceCollection.AddOptions<ApiKeys>();
        builderAction?.Invoke(builder);
        serviceCollection.TryAddSingleton<IApiKeyAuthenticator, ApiKeyAuthenticator>();
        serviceCollection.AddSingleton<IRequestAuthenticator>(pr => pr.GetRequiredService<IApiKeyAuthenticator>());
        serviceCollection.AddStartupActivator<IApiKeyAuthenticator>();
        return serviceCollection;
    }

    public static IServiceCollection AddAllowedCorsOriginsFilter(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddOptions<AllowAccess>()
            .Bind(configuration.GetSection(AllowAccess.ConfigSection));
        serviceCollection.AddSingleton<IAllowedCorsOriginsFilter, AllowedCorsOriginsFilter>();
        serviceCollection.AddStartupActivator<IAllowedCorsOriginsFilter>();
        return serviceCollection;
    }

    /// <summary>
    ///     Adds an action to be run when RESTable is configured
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static IServiceCollection AddOnConfigureRESTable(this IServiceCollection serviceCollection, Action<IServiceProvider> action)
    {
        serviceCollection.AddTransient<IStartupActivator>(pr => new StartupActivator<IApplicationServiceProvider>(pr, provider =>
        {
            action(provider);
            return Task.CompletedTask;
        }));
        return serviceCollection;
    }

    /// <summary>
    ///     Adds a StartupActivator for the given service, making sure it's activated during startup of the app, when RESTable
    ///     is configured
    /// </summary>
    public static IServiceCollection AddStartupActivator<TService>(this IServiceCollection serviceCollection) where TService : class
    {
        serviceCollection.AddTransient<IStartupActivator>(pr => new StartupActivator<TService>(pr, _ => Task.CompletedTask));
        return serviceCollection;
    }

    /// <summary>
    ///     Adds a StartupActivator for the given task, making sure it's called during startup of the app, when RESTable is
    ///     configured
    /// </summary>
    public static IServiceCollection AddStartupActivator<TService>(this IServiceCollection serviceCollection, Func<TService, Task> activator) where TService : class
    {
        serviceCollection.AddTransient<IStartupActivator>(pr => new StartupActivator<TService>(pr, activator));
        return serviceCollection;
    }

    public static IServiceCollection AddRESTable(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddJson();

        serviceCollection.AddOptions<RESTableConfiguration>().BindConfiguration(RESTableConfiguration.ConfigSection, o => o.BindNonPublicProperties = true);
        serviceCollection.AddHostedService<RESTableInitializer>();

        var terminalSubject = new Subject<Terminal>();
        serviceCollection.AddSingleton(new TerminalSubjectAccessor(terminalSubject));
        serviceCollection.AddSingleton(typeof(ITerminalObservable<>), typeof(TerminalObservable<>));
        serviceCollection.AddSingleton(typeof(ITerminalObservable), typeof(TerminalObservable));

        serviceCollection.AddSingleton<IApplicationServiceProvider>(sp => new ApplicationServiceProvider(sp));
        serviceCollection.TryAddSingleton<WebSocketManager>();
        serviceCollection.TryAddSingleton<RESTableConfiguration>();
        serviceCollection.TryAddSingleton(pr => pr.GetRequiredService<TypeCache>().TermFactory);
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
        serviceCollection.AddSingleton<IAllowAllAuthenticator, AllowAllAuthenticator>();
        serviceCollection.TryAddSingleton<IRequestAuthenticator>(pr => pr.GetRequiredService<IAllowAllAuthenticator>());
        serviceCollection.TryAddSingleton<IAllowedCorsOriginsFilter, AllCorsOriginsAllowed>();
        serviceCollection.TryAddSingleton<RootAccess>();
        serviceCollection.TryAddSingleton<RootClient>();
        serviceCollection.TryAddTransient(pr =>
        {
            var rootClient = pr.GetRequiredService<RootClient>();
            return new RootContext(rootClient, pr);
        });
        serviceCollection.AddSingleton<IEntityResourceProvider, InMemoryEntityResourceProvider>();
        serviceCollection.AddSingleton<IProtocolProvider, DefaultProtocolProvider>();
        serviceCollection.AddSingleton(typeof(EntityOperations<>));
        serviceCollection.AddTransient(typeof(ICombinedTerminal<>), typeof(CombinedTerminal<>));
        serviceCollection.AddTransient(typeof(ITerminalCollection<>), typeof(TerminalCollection<>));
        serviceCollection.AddSingleton(typeof(ISerializationMetadata<>), typeof(SerializationMetadata<>));
        serviceCollection.AddSingleton(typeof(ISerializationMetadataAccessor), pr =>
        {
            ISerializationMetadata accessor(Type type)
            {
                return (ISerializationMetadata) pr.GetRequiredService(typeof(ISerializationMetadata<>).MakeGenericType(type));
            }

            return new SerializationMetadataAccessor(accessor);
        });
        return serviceCollection;
    }
}
