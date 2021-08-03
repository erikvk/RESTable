using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;
using static RESTable.Json.DefaultConverterOperations;

namespace RESTable.Json
{
    /// <summary>
    /// Converter for declared types that have at least one RESTableMemberAttribute on some
    /// member.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DefaultDeclaredConverter<T> : JsonConverter<T>
    {
        private TypeCache TypeCache { get; }
        private IReadOnlyDictionary<string, DeclaredProperty> DeclaredProperties { get; }
        private Constructor<T>? ParameterLessConstructor { get; }
        private DeclaredProperty[] VisibleDeclaredPropertiesArray { get; }

        public DefaultDeclaredConverter(TypeCache typeCache)
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
                case JsonTokenType.EndObject:
                {
                    return ParameterLessConstructor() ?? throw new Exception($"Could not instantiate type '{typeToConvert.GetRESTableTypeName()}' while reading JSON");
                }
                case JsonTokenType.PropertyName:
                {
                    var instance = ParameterLessConstructor() ?? throw new Exception($"Could not instantiate type '{typeToConvert.GetRESTableTypeName()}' while reading JSON");
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        var propertyName = reader.GetString() ?? throw new JsonException("Invalid JSON token encountered. Expected property name.");
                        if (!DeclaredProperties.TryGetValue(propertyName, out var property))
                        {
                            // Encountered an unknown property in input JSON. Skipping.
                            reader.Skip();
                        }
                        else
                        {
                            var value = JsonSerializer.Deserialize(ref reader, property!.Type, options);
                            var setValueTask = property.SetValue(instance, value);
                            if (setValueTask.IsCompleted)
                                setValueTask.GetAwaiter().GetResult();
                            else setValueTask.AsTask().Wait();
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
            WriteDeclaredMembers(writer, VisibleDeclaredPropertiesArray, value, options);
            writer.WriteEndObject();
        }

        public override bool HandleNull => true;
    }
}