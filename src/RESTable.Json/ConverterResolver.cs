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
        private HashSet<JsonConverter> CustomConverters { get; }
        private IReadOnlyDictionary<Type, JsonConverter> BuiltInConverters { get; }
        private ISerializationMetadataAccessor MetadataAccessor { get; }

        public ConverterResolver(TypeCache typeCache, JsonSerializerOptionsAccessor optionsAccessor, ISerializationMetadataAccessor metadataAccessor)
        {
            TypeCache = typeCache;
            CustomConverters = optionsAccessor.Options.Converters.ToHashSet();
            BuiltInConverters = new Dictionary<Type, JsonConverter>
            {
                [typeof(TypeConverter)] = new TypeConverter(),
                [typeof(HeadersConverter)] = new HeadersConverter(),
                [typeof(ContentTypeConverter)] = new ContentTypeConverter(),
                [typeof(ContentTypesConverter)] = new ContentTypesConverter(),
                [typeof(ToStringConverter)] = new ToStringConverter(),
                [typeof(VersionConverter)] = new VersionConverter(),
                [typeof(JsonStringEnumConverter)] = new JsonStringEnumConverter(),
                [typeof(ProcessedEntityConverter)] = new ProcessedEntityConverter()
            };
            MetadataAccessor = metadataAccessor;
        }

        private Type? GetConverterType(Type objectType) => objectType switch
        {
            _ when CustomConverters.FirstOrDefault(c => c.CanConvert(objectType)) is { } converter => converter.GetType(),
            _ when objectType.HasAttribute(out JsonConverterAttribute? a) && a!.ConverterType is Type converterType => converterType,
            _ when objectType.IsSubclassOf(typeof(Type)) => typeof(TypeConverter),
            _ when objectType == typeof(Headers) => typeof(HeadersConverter),
            _ when objectType == typeof(ContentType) => typeof(ContentTypeConverter),
            _ when objectType == typeof(ContentTypes) => typeof(ContentTypesConverter),
            _ when objectType == typeof(Term) => typeof(ToStringConverter),
            _ when objectType == typeof(Version) => typeof(VersionConverter),
            _ when objectType.IsEnum => typeof(JsonStringEnumConverter),
            _ when objectType == typeof(ProcessedEntity) => typeof(ProcessedEntityConverter),
            _ when objectType.IsDictionary() => typeof(DefaultDynamicConverter<>),
            _ when TypeCache.GetDeclaredProperties(objectType).Values.All(p => !p.HasAttribute<RESTableMemberAttribute>()) => null,
            _ => typeof(DefaultDeclaredConverter<>)
        };

        public override bool CanConvert(Type typeToConvert)
        {
            return GetConverterType(typeToConvert) is not null;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = GetConverterType(typeToConvert);
            if (converterType is null)
                return null;
            var match = options.Converters.FirstOrDefault(c => converterType == c.GetType());
            if (match is null)
                BuiltInConverters.TryGetValue(converterType, out match);
            if (match is null && converterType.IsGenericTypeDefinition)
            {
                converterType = converterType.MakeGenericType(typeToConvert);
                var metadata = MetadataAccessor.GetMetadata(typeToConvert);
                match = (JsonConverter?) Activator.CreateInstance(converterType, metadata);
            }
            if (match is JsonConverterFactory factory)
            {
                match = factory.CreateConverter(typeToConvert, options);
            }
            return match;
        }
    }
}