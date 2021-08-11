using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
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
    public class ConverterResolver : JsonConverterFactory
    {
        private TypeCache TypeCache { get; }
        private IReadOnlyDictionary<Type, JsonConverter> BuiltInConverters { get; }
        private IApplicationServiceProvider ServiceProvider { get; }

        public ConverterResolver(TypeCache typeCache, IApplicationServiceProvider serviceProvider)
        {
            TypeCache = typeCache;
            ServiceProvider = serviceProvider;
            BuiltInConverters = new Dictionary<Type, JsonConverter>
            {
                [typeof(HeadersConverter)] = new HeadersConverter(),
                [typeof(ContentTypeConverter)] = new ContentTypeConverter(),
                [typeof(ContentTypesConverter)] = new ContentTypesConverter(),
                [typeof(ToStringConverter)] = new ToStringConverter(),
                [typeof(VersionConverter)] = new VersionConverter(),
                [typeof(JsonStringEnumConverter)] = new JsonStringEnumConverter(),
                [typeof(ProcessedEntityConverter)] = new ProcessedEntityConverter()
            };
        }

        public override bool CanConvert(Type typeToConvert) => GetConverterType(typeToConvert) is not null;

        private static Type? GetConverterType(Type objectType) => objectType switch
        {
            // Attributes are respected
            _ when objectType.HasAttribute(out JsonConverterAttribute? a) => a!.ConverterType,

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

            // Types that have no properties decorated with RESTableMemberAttribute are skipped.
            _ when objectType.GetProperties().All(property => !property.HasAttribute<RESTableMemberAttribute>()) => null,

            // We also always skip enumerable types.
            _ when objectType.ImplementsEnumerableInterface(out _) => null,

            // And for the remaining types, we use the default converter for declared properties only.
            _ => typeof(DefaultDeclaredConverter<>).MakeGenericType(objectType)
        };

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = GetConverterType(typeToConvert);
            if (converterType is null)
                return null;
            var match = options.Converters.FirstOrDefault(c => converterType == c.GetType());
            if (match is null)
                BuiltInConverters.TryGetValue(converterType, out match);
            if (match is null && converterType.IsGenericType)
            {
                match = (JsonConverter?) ActivatorUtilities.CreateInstance(ServiceProvider, converterType);
            }
            if (match is JsonConverterFactory factory)
            {
                match = factory.CreateConverter(typeToConvert, options);
            }
            return match;
        }
    }
}