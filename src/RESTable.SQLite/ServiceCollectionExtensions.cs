using System;
using Microsoft.Extensions.Options;
using RESTable.Resources;
using RESTable.Sqlite;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteProvider(this IServiceCollection serviceCollection, Action<OptionsBuilder<SqliteOptions>>? builderAction = null)
    {
        var builder = serviceCollection
            .AddOptions<SqliteOptions>();
        builderAction?.Invoke(builder);
        serviceCollection.AddSingleton<IEntityResourceProvider, SqliteEntityResourceProvider>();
        return serviceCollection;
    }
}
