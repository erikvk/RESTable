using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Json.Converters;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Requests.Processors;
using RESTable.Resources;

namespace RESTable.Json
{
    /// <summary>
    /// Resolves a converter type for all types that RESTable defines operations for,
    /// and acts as a top-level resolver for these converters.
    /// </summary>
    internal class ConverterResolver : JsonConverterFactory
    {
        private TypeCache TypeCache { get; }
        private IReadOnlyDictionary<Type, JsonConverter> InstantiatedConverters { get; }
        private IApplicationServiceProvider ServiceProvider { get; }
        private IRegisteredGenericJsonConverterType[] GenericJsonConverterTypes { get; }

        public ConverterResolver(TypeCache typeCache, IApplicationServiceProvider serviceProvider, IEnumerable<IRegisteredGenericJsonConverterType> genericConverterTypes)
        {
            TypeCache = typeCache;
            ServiceProvider = serviceProvider;
            GenericJsonConverterTypes = genericConverterTypes.ToArray();
            InstantiatedConverters = new Dictionary<Type, JsonConverter>
            {
                [typeof(HeadersConverter)] = new HeadersConverter(),
                [typeof(ContentTypeConverter)] = new ContentTypeConverter(),
                [typeof(ContentTypesConverter)] = new ContentTypesConverter(),
                [typeof(ToStringConverter)] = new ToStringConverter(),
                [typeof(VersionConverter)] = new VersionConverter(),
                [typeof(JsonStringEnumConverter)] = new JsonStringEnumConverter()
            };
        }

        public override bool CanConvert(Type typeToConvert) => GetConverterType(typeToConvert) is not null;

        private bool HasGenericConverterType(Type typeToConvert, out IRegisteredGenericJsonConverterType? genericJsonConverterType)
        {
            foreach (var registeredGenericJsonConverterType in GenericJsonConverterTypes)
            {
                if (registeredGenericJsonConverterType.CanConvert(typeToConvert))
                {
                    genericJsonConverterType = registeredGenericJsonConverterType;
                    return true;
                }
            }
            genericJsonConverterType = null;
            return false;
        }

        private Type? GetConverterType(Type objectType) => objectType switch
        {
            // Attributes are respected
            _ when objectType.HasAttribute(out JsonConverterAttribute? a) => a!.ConverterType,

            // Try to match against a registered generic json converter, if any
            _ when HasGenericConverterType(objectType, out var genericJsonConverterType) => genericJsonConverterType!.GetConverterType(objectType),

            // Types and resources are given a special treatment
            _ when typeof(Type).IsAssignableFrom(objectType) => typeof(TypeConverter<>).MakeGenericType(objectType),
            _ when typeof(IResource).IsAssignableFrom(objectType) => typeof(ResourceConverter<>).MakeGenericType(objectType),

            // We map some types to the RESTable built-in converters
            _ when objectType == typeof(Headers) => typeof(HeadersConverter),
            _ when objectType == typeof(ContentType) => typeof(ContentTypeConverter),
            _ when objectType == typeof(ContentTypes) => typeof(ContentTypesConverter),
            _ when objectType == typeof(Term) => typeof(ToStringConverter),
            _ when objectType == typeof(Version) => typeof(VersionConverter),
            _ when objectType.IsEnum => typeof(JsonStringEnumConverter),
            _ when objectType == typeof(ProcessedEntity) => typeof(ProcessedEntityConverter),

            // Dictionary types get their own converters. Read-only dictionaries get their own that do not
            // put any dictionary entries in when deserializing. If the key is not string, it is treated as
            // read only, and we call ToString() on the key object.
            _ when objectType.IsDictionary(out var writable, out var keyType, out var valueType) => writable && keyType == typeof(string)
                ? typeof(DefaultDictionaryConverter<,>).MakeGenericType(objectType, valueType!)
                : typeof(DefaultReadonlyDictionaryConverter<,,>).MakeGenericType(objectType, keyType!, valueType!),

            // Types that would not benefit from the RESTable converters are skipped
            _ when objectType.GetProperties().All(property =>
                !property.HasAttribute<RESTableMemberAttribute>()
            ) => null,

            // We also always skip enumerable types.
            _ when objectType.ImplementsEnumerableInterface(out _) => null,

            // And for the remaining types, we use the default converter for declared properties only.
            _ => typeof(DefaultDeclaredConverter<>).MakeGenericType(objectType)
        };

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var match = options.Converters
                .Where(c => c is not ConverterResolver)
                .FirstOrDefault(c => c.CanConvert(typeToConvert));
            var resolvedConverterType = GetConverterType(typeToConvert);
            if (match is null && resolvedConverterType is null)
            {
                // There is no custom converter for this type, and this resolver can not
                // deal with it either. let's use the default converters
                return null;
            }

            if (match is null)
            {
                // We know that resolvedConverterType is not null.
                // Let's see if we can use one of the built-in converters
                InstantiatedConverters.TryGetValue(resolvedConverterType!, out match);
            }
            if (match is JsonConverterFactory factory)
            {
                match = factory.CreateConverter(typeToConvert, options);
            }
            if (match is null)
            {
                match = (JsonConverter?) ActivatorUtilities.CreateInstance(ServiceProvider, resolvedConverterType!);
            }
            return match;
        }
    }
}