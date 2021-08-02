using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.ContentTypeProviders;
using RESTable.Json;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJson(this IServiceCollection serviceCollection, Action<JsonSerializerOptions>? jsonOptionsAction = null)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = false,
                IncludeFields = false,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            jsonOptionsAction?.Invoke(jsonOptions);
            var newOptions = new JsonSerializerOptions(jsonOptions);
            serviceCollection.AddSingleton(new JsonSerializerOptionsAccessor(newOptions));
            serviceCollection.AddSingleton<ConverterResolver>();
            serviceCollection.AddSingleton<IJsonProvider, SystemTextJsonProvider>();
            serviceCollection.AddSingleton<IContentTypeProvider>(pr => pr.GetRequiredService<IJsonProvider>());
            return serviceCollection;
        }
    }
}