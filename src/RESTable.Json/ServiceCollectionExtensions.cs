using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            jsonOptionsAction?.Invoke(jsonOptions);
            var newOptions = new JsonSerializerOptions(jsonOptions);
            serviceCollection.AddJsonConverter<ConverterResolver>();
            serviceCollection.AddSingleton<IJsonProvider, SystemTextJsonProvider>();

            // There is a circular dependency between SystemTextJsonProvider, which needs the converters, and the converters,
            // which must be able to inject the SystemTextJsonProvider. To solve this, we put together the list of converters
            // separately from activating the SystemTextJsonProvider, and call SetOptions() for it after activation, during
            // RESTable configuration.
            serviceCollection.AddOnConfigureRESTable(sp =>
            {
                foreach (var converter in sp.GetServices<IRegisteredJsonConverter>().Select(rjc => rjc.GetInstance(sp)))
                    newOptions.Converters.Add(converter);
                var jsonProvider = (SystemTextJsonProvider) sp.GetRequiredService<IJsonProvider>();
                jsonProvider.SetOptions(newOptions);
            });

            serviceCollection.AddSingleton<IContentTypeProvider>(pr => pr.GetRequiredService<IJsonProvider>());
            return serviceCollection;
        }

        /// <summary>
        /// Adds a JsonConverter type to be used with RESTable.Json. The instance will be created when the IJsonProvider
        /// is instantiated, and any dependencies resolved from the service provider.
        /// </summary>
        public static IServiceCollection AddJsonConverter<T>(this IServiceCollection serviceCollection) where T : JsonConverter
        {
            serviceCollection.AddSingleton<IRegisteredJsonConverter>(new RegisteredJsonConverter(typeof(T)));
            return serviceCollection;
        }

        /// <summary>
        /// Adds a JsonConverter type to be used with RESTable.Json. The instance will be created when the IJsonProvider
        /// is instantiated, and any dependencies resolved from the service provider.
        /// </summary>
        public static IServiceCollection AddJsonConverter<T>(this IServiceCollection serviceCollection, T instance) where T : JsonConverter
        {
            serviceCollection.AddSingleton<IRegisteredJsonConverter>(new RegisteredJsonConverter(instance));
            return serviceCollection;
        }
    }
}