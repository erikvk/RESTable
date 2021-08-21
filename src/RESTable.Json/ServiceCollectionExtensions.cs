using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable;
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
                foreach (var registeredJsonConverter in sp.GetServices<IRegisteredJsonConverter>())
                {
                    var converter = registeredJsonConverter.GetInstance(sp);
                    newOptions.Converters.Add(converter);
                }
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
        public static IServiceCollection AddJsonConverter(this IServiceCollection serviceCollection, JsonConverter instance)
        {
            serviceCollection.AddSingleton<IRegisteredJsonConverter>(new RegisteredJsonConverter(instance));
            return serviceCollection;
        }

        /// <summary>
        /// Adds a JsonConverter type to be used with RESTable.Json. The instance will be created when the IJsonProvider
        /// is instantiated, and any dependencies resolved from the service provider.
        /// </summary>
        public static IServiceCollection AddJsonConverter(this IServiceCollection serviceCollection, Type converterType)
        {
            if (!typeof(JsonConverter).IsAssignableFrom(converterType))
            {
                throw new InvalidOperationException($"Cannot add type '{converterType.GetRESTableTypeName()}' as JsonConverter, since it is " +
                                                    $"not a subclass of '{typeof(JsonConverter).FullName}'");
            }
            serviceCollection.AddSingleton<IRegisteredJsonConverter>(new RegisteredJsonConverter(converterType));
            return serviceCollection;
        }

        /// <summary>
        /// Adds a generic JsonConverter type to be used with RESTable.Json, that will be instantiated for each type that the given predicate
        /// holds for. The instance will be created when the IJsonProvider is instantiated, and any dependencies resolved from the service provider.
        /// The converter will be used for all types that pass the canConvert predicate. If null, the constraints on the generic type of the
        /// generic converter type will be used to determine which types to assign to this converter.
        /// </summary>
        public static IServiceCollection AddGenericJsonConverter(this IServiceCollection serviceCollection, Type genericConverterType, Predicate<Type>? canConvert = null)
        {
            if (!genericConverterType.IsGenericTypeDefinition || genericConverterType.GetGenericArguments() is not {Length: 1} argumentArray)
            {
                throw new InvalidOperationException($"Cannot add type '{genericConverterType.GetRESTableTypeName()}' as a generic JsonConverter type. It must be a " +
                                                    "generic type definition with exactly one generic argument");
            }
            if (!typeof(JsonConverter).IsAssignableFrom(genericConverterType))
            {
                throw new InvalidOperationException($"Cannot add type '{genericConverterType.GetRESTableTypeName()}' as a generic JsonConverter type, since it is " +
                                                    $"not a subclass of '{typeof(JsonConverter).FullName}'");
            }
            
            bool defaultCanConvert(Type type)
            {
                try
                {
                    _ = genericConverterType.MakeGenericType(type);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            serviceCollection.AddSingleton<IRegisteredGenericJsonConverterType>(new RegisteredGenericJsonConverterType(genericConverterType, canConvert ?? defaultCanConvert));

            return serviceCollection;
        }
    }
}