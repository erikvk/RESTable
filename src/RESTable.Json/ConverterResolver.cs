using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;
using RESTable.Requests;
using TermCache = RESTable.Admin.TermCache;

namespace RESTable.Json
{
    public class ConverterResolver : JsonConverterFactory
    {
        private TermCache TermCache { get; }
        private JsonStringEnumConverter JsonStringEnumConverter { get; }

        public ConverterResolver(TermCache termCache)
        {
            TermCache = termCache;
            JsonStringEnumConverter = new JsonStringEnumConverter();
        }

        private Type? GetConverterType(Type objectType) => objectType switch
        {
            _ when objectType.IsSubclassOf(typeof(Type)) => typeof(TypeConverter),
            _ when objectType == typeof(Headers) => typeof(HeadersConverter),
            _ when objectType == typeof(ContentType) => typeof(ContentTypeConverter),
            _ when objectType == typeof(ContentTypes) => typeof(ContentTypesConverter),
            _ when objectType == typeof(Term) => typeof(ToStringConverter),
            _ when objectType == typeof(Version) => typeof(VersionConverter),
            _ when objectType.IsEnum => typeof(JsonStringEnumConverter),
            _ => null
        };

        public override bool CanConvert(Type typeToConvert)
        {
            return GetConverterType(typeToConvert) is not null;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = GetConverterType(typeToConvert);
            if (converterType is null) return null;
            var converter = options.Converters.FirstOrDefault(c => converterType == c.GetType()) ?? throw new Exception("Could not find expected converter");
            return converter;
        }
    }
}