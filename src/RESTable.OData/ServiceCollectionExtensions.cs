using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.OData;
using RESTable.ProtocolProviders;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddODataProvider(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IProtocolProvider, ODataProtocolProvider>();
            return serviceCollection;
        }
    }
}