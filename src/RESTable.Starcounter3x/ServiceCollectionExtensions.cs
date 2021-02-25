using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.Resources;
using RESTable.Starcounter3x;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddStarcounterProvider(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IEntityResourceProvider, StarcounterDeclaredResourceProvider>();
            serviceCollection.TryAddSingleton<IEntityTypeContractResolver, StarcounterEntityTypeContractResolver>();
            return serviceCollection;
        }
    }
}