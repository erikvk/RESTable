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

        private static IEnumerable<JsonConverter> GetBuiltInConverters()
        {
            yield return new TypeConverter();
            yield return new HeadersConverter();
            yield return new ContentTypeConverter();
            yield return new ContentTypesConverter();
            yield return new ToStringConverter();
            yield return new VersionConverter();
            yield return new JsonStringEnumConverter();
            yield return new ProcessedEntityConverter();
        }

        public ConverterResolver(TypeCache typeCache, JsonSerializerOptionsAccessor optionsAccessor)
        {
            TypeCache = typeCache;
            CustomConverters = optionsAccessor.Options.Converters.ToHashSet();
            foreach (var converter in GetBuiltInConverters())
            {
                optionsAccessor.Options.Converters.Add(converter);
            }
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
            _ when objectType.IsDynamic() => typeof(DefaultDynamicConverter<>).MakeGenericType(objectType),
            _ when TypeCache.GetDeclaredProperties(objectType).Values.All(p => !p.HasAttribute<RESTableMemberAttribute>()) => null,
            _ => typeof(DefaultDeclaredConverter<>).MakeGenericType(objectType)
        };

        public override bool CanConvert(Type typeToConvert) => GetConverterType(typeToConvert) is not null;

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = GetConverterType(typeToConvert);
            if (converterType is null)
                return null;
            var match = options.Converters.FirstOrDefault(c => converterType == c.GetType());
            if (match is null)
            {
                if (converterType.IsGenericType)
                    return (JsonConverter?) Activator.CreateInstance(converterType, TypeCache);
                throw new Exception("Could not find expected converter");
            }
            return match;
        }
    }
}