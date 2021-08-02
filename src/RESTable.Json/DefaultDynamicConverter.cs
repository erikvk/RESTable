using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;
using static RESTable.Json.DefaultConverterOperations;

namespace RESTable.Json
{
    public class DefaultDynamicConverter<T> : JsonConverter<T> where T : IDictionary<string, object?>
    {
        private TypeCache TypeCache { get; }
        private IReadOnlyDictionary<string, DeclaredProperty> DeclaredProperties { get; }
        private Constructor<T>? ParameterLessConstructor { get; }
        private DeclaredProperty[] VisibleDeclaredPropertiesArray { get; }

        public DefaultDynamicConverter(TypeCache typeCache)
        {
            TypeCache = typeCache;
            DeclaredProperties = typeCache.GetDeclaredProperties(typeof(T));
            VisibleDeclaredPropertiesArray = DeclaredProperties.Values
                .Where(p => !p.Hidden)
                .OrderBy(p => p.Order)
                .ToArray();
            ParameterLessConstructor = typeof(T).MakeStaticConstructor<T>();
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (ParameterLessConstructor is null)
            {
                // We don't support creating objects through a non-default constructor for now.
                return default;
            }

            reader.Read();
            switch (reader.TokenType)
            {
                case JsonTokenType.Null: return default;
                case JsonTokenType.PropertyName:
                {
                    var instance = ParameterLessConstructor();
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        var propertyName = reader.GetString() ?? throw new JsonException("Invalid JSON token encountered. Expected property name.");
                        if (DeclaredProperties.TryGetValue(propertyName, out var property))
                        {
                            SetDeclaredMember(ref reader, property!, instance, options);
                        }
                        else
                        {
                            // The read property is not declared, let's add it as dynamic
                            SetDynamicMember(ref reader, propertyName, instance, options);
                        }
                        reader.Read();
                    }
                    return instance;
                }
                default: throw new JsonException("Invalid JSON token encountered");
            }
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            WriteDynamicMembers(writer, value, options);
            WriteDeclaredMembers(writer, VisibleDeclaredPropertiesArray, value, options);
            writer.WriteEndObject();
        }

        public override bool HandleNull => true;
    }
}