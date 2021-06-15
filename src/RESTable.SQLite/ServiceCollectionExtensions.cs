using RESTable.Resources;
using RESTable.SQLite;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqliteProvider(this IServiceCollection serviceCollection, string dbPath)
        {
            serviceCollection.AddSingleton<IEntityResourceProvider>(new SQLiteEntityResourceProvider(dbPath));
            return serviceCollection;
        }
    }
}