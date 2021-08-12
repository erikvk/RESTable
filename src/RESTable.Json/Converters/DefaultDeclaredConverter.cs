using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using RESTable.Meta;
using static RESTable.Json.Converters.DefaultConverterOperations;

namespace RESTable.Json.Converters
{
    /// <summary>
    /// Converter for declared types that have at least one RESTableMemberAttribute on some
    /// member.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [BuiltInConverter]
    public class DefaultDeclaredConverter<T> : JsonConverter<T>
    {
        private ISerializationMetadata<T> Metadata { get; }

        public DefaultDeclaredConverter(ISerializationMetadata<T> metadata)
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
                        if (reader.TokenType is not JsonTokenType.PropertyName || reader.GetString() is not string propertyName)
                            throw new JsonException("Invalid JSON token encountered. Expected property name.");

                        if (Metadata.GetProperty(propertyName) is not { } property)
                        {
                            // Encountered an unknown property in input JSON. Skipping.
                            reader.Skip();
                        }
                        else
                        {
                            var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
                            var setValueTask = property.SetValue(instance!, value);
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
            SerializeDeclaredMembers(writer, Metadata, value, options);
            writer.WriteEndObject();
        }

        public override bool HandleNull => true;
    }
}