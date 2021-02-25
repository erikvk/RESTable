using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using RESTable.ContentTypeProviders;
using RESTable.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonProvider(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IJsonProvider, NewtonsoftJsonProvider>();
            serviceCollection.TryAddSingleton<IContentTypeProvider>(pr => pr.GetService<IJsonProvider>());
            serviceCollection.TryAddSingleton<JsonSerializer>(pr => pr.GetService<IJsonProvider>().GetSerializer());
            return serviceCollection;
        }
    }
}