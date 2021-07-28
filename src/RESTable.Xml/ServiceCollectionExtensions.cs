using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.ContentTypeProviders;
using RESTable.Xml;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddXmlProvider(this IServiceCollection serviceCollection, Action<XmlSettings>? xmlSettingsAction = null)
        {
            var xmlSettings = new XmlSettings();
            xmlSettingsAction?.Invoke(xmlSettings);
            serviceCollection.AddSingleton<XmlSettings>(xmlSettings);
            serviceCollection.AddJson();
            serviceCollection.TryAddSingleton<IContentTypeProvider, XmlContentTypeProvider>();
            return serviceCollection;
        }
    }
}