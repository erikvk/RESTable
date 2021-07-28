using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RESTable.ContentTypeProviders;
using RESTable.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using RESTable;
using RESTable.Requests;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJson(this IServiceCollection serviceCollection, Action<JsonSerializerOptions>? jsonOptionsAction = null)
        {
            var jsonSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = false,
                IncludeFields = false,
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            jsonOptionsAction?.Invoke(jsonSettings);
            serviceCollection.AddSingleton<JsonSerializerOptions>(jsonSettings);
            serviceCollection.TryAddSingleton<IJsonProvider, SystemTextJsonProvider>();
            serviceCollection.AddSingleton<IContentTypeProvider>(pr => pr.GetRequiredService<IJsonProvider>());
            return serviceCollection;
        }
    }
}