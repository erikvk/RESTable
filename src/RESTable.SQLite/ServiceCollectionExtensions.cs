using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.Resources;
using RESTable.SQLite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqliteProvider(this IServiceCollection serviceCollection, string databasePath)
        {
            serviceCollection.TryAddSingleton<IEntityResourceProvider>(new SQLiteEntityResourceProvider(databasePath));
            return serviceCollection;
        }
    }
}