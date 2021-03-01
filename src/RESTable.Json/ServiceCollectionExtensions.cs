using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RESTable.ContentTypeProviders;
using RESTable.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJsonProvider(this IServiceCollection serviceCollection, Action<JsonSettings> jsonSettingsAction = null)
        {
            var jsonSettings = new JsonSettings();
            jsonSettingsAction?.Invoke(jsonSettings);
            serviceCollection.AddSingleton<JsonSettings>(jsonSettings);
            serviceCollection.TryAddSingleton<IContractResolver, DefaultResolver>();
            serviceCollection.TryAddSingleton<IJsonProvider, NewtonsoftJsonProvider>();
            serviceCollection.TryAddSingleton<IContentTypeProvider>(pr => pr.GetService<IJsonProvider>());
            serviceCollection.TryAddSingleton<JsonSerializer>(pr => pr.GetService<IJsonProvider>().GetSerializer());
            return serviceCollection;
        }
    }
}