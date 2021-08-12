using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;
using static RESTable.Json.Converters.DefaultConverterOperations;

namespace RESTable.Json.Converters
{
    /// <summary>
    /// Converter for declared types that are dictionaries, defined as implementing IEnumerable{T} where
    /// T is some KeyValuePair{TKey,TValue} type.
    /// member.
    /// </summary>
    [BuiltInConverter]
    public class DefaultReadonlyDictionaryConverter<T, TKey, TValue> : JsonConverter<T> where T : IEnumerable<KeyValuePair<TKey, TValue?>> where TKey : notnull
    {
        private ISerializationMetadata<T> Metadata { get; }

        public DefaultReadonlyDictionaryConverter(ISerializationMetadata<T> metadata)
        {
            Metadata = metadata;
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();

            switch (reader.TokenType)
            {
                case JsonTokenType.Null: return default;
                case JsonTokenType.EndObject:
                {
                    return Metadata.CreateInstance();
                }
                case JsonTokenType.PropertyName:
                {
                    var instance = Metadata.CreateInstance();
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.GetString() is not string propertyName)
                            throw new JsonException("Invalid JSON token encountered. Expected property name.");

                        if (Metadata.GetProperty(propertyName) is not { } property)
                        {
                            // This is a readonly dictionary, so we can't add dynamic members. Instead we skip.
                        }
                        else
                        {
                            // Property is declared, set it using the property's set value task
                            SetDeclaredMember(ref reader, property, instance, options);
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
            WriteDynamicMembers<T, TKey, TValue>(writer, value, options);
            SerializeDeclaredMembers(writer, Metadata, value, options);
            writer.WriteEndObject();
        }

        public override bool HandleNull => true;
    }
}