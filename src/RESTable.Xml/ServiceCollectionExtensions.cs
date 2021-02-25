using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.ContentTypeProviders;
using RESTable.Xml;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddXmlProvider(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddJsonProvider();
            serviceCollection.TryAddSingleton<IContentTypeProvider, XmlContentTypeProvider>();
            return serviceCollection;
        }
    }
}