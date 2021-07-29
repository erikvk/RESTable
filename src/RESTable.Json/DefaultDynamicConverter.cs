using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;

namespace RESTable.Json
{
    public class DefaultDynamicConverter<T> : JsonConverter<T> where T : IDictionary<string, object?>, new()
    {
        private TypeCache TypeCache { get; }
        private IReadOnlyDictionary<string, DeclaredProperty> DeclaredProperties { get; }

        internal DefaultDynamicConverter(TypeCache typeCache)
        {
            TypeCache = typeCache;
            DeclaredProperties = typeCache.GetDeclaredProperties(typeof(T));
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new T();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) { }
    }
}