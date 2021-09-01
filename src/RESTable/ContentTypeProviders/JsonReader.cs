using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;
using RESTable.Meta;

namespace RESTable.ContentTypeProviders
{
    public readonly struct JsonReader
    {
        private JsonSerializerOptions options { get; }
        private IJsonProvider JsonProvider { get; }
        private ArrayPool<(bool, object?)> ArrayPool { get; }

        public JsonReader(JsonSerializerOptions jsonSerializerOptions, IJsonProvider jsonProvider)
        {
            options = jsonSerializerOptions;
            JsonProvider = jsonProvider;
            ArrayPool = ArrayPool<(bool, object?)>.Shared;
        }

        #region Generic

        public bool TryReadNextProperty<T>(ref Utf8JsonReader reader, out string? name, out T? value)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType != JsonTokenType.PropertyName && !reader.Read() ||
                reader.TokenType == JsonTokenType.EndObject
            )
            {
                name = null;
                value = default;
                return false;
            }
            if
            (
                reader.TokenType != JsonTokenType.PropertyName ||
                reader.GetString() is not string propertyName
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected PropertyName");
            }

            name = propertyName;
            value = JsonSerializer.Deserialize<T>(ref reader, options);
            reader.Read();
            return true;
        }

        public T? ReadToObjectOrGetDefault<T>(ref Utf8JsonReader reader, ISerializationMetadata<T> metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }
            return (T) ReadToObjectInternal(ref reader, metadata);
        }

        public T ReadToObject<T>(ref Utf8JsonReader reader, ISerializationMetadata<T> metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new NullReferenceException("Expected a non-null value when reading to object");
            }
            return (T) ReadToObjectInternal(ref reader, metadata);
        }

        public void ReadToObject<T>(ref Utf8JsonReader reader, T instance, ISerializationMetadata metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                {
                    // No data to read
                    return;
                }
                case JsonTokenType.PropertyName:
                {
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType is not JsonTokenType.PropertyName || reader.GetString() is not string propertyName)
                            throw new JsonException("Invalid JSON token encountered. Expected property name.");

                        if (metadata.GetProperty(propertyName) is not { } property)
                        {
                            // Encountered an unknown property in input JSON. Skipping.
                            reader.Skip();
                        }
                        else
                        {
                            // Property is declared, set it using the property's set value task
                            ReadAndSetDeclaredMember(ref reader, property, instance!, options);
                        }
                        reader.Read();
                    }
                    return;
                }
                default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
            }
        }

        public TDictionary? ReadToDictionaryOrGetDefault<TDictionary, TValue>(ref Utf8JsonReader reader, ISerializationMetadata<TDictionary> metadata)
            where TDictionary : IDictionary<string, TValue?>
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered.");
            }
            if (reader.TokenType == JsonTokenType.Null)
                return default;
            var instance = metadata.InvokeParameterlessConstructor();
            ReadToDictionary(ref reader, instance, metadata);
            return instance;
        }

        public TDictionary ReadToDictionary<TDictionary, TValue>(ref Utf8JsonReader reader, ISerializationMetadata<TDictionary> metadata)
            where TDictionary : IDictionary<string, TValue?>
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered.");
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new NullReferenceException("Expected a non-null value when reading to dictionary");
            }
            var instance = metadata.InvokeParameterlessConstructor();
            ReadToDictionary(ref reader, instance, metadata);
            return instance;
        }

        public void ReadToDictionary<TValue>(ref Utf8JsonReader reader, IDictionary<string, TValue?> instance, ISerializationMetadata metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject: return;
                case JsonTokenType.PropertyName:
                {
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.GetString() is not string propertyName)
                            throw new JsonException("Invalid JSON token encountered. Expected property name.");

                        if (metadata.GetProperty(propertyName) is not { } property)
                        {
                            // The read property is not declared, let's add it as dynamic
                            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
                            instance[propertyName] = JsonProvider.ToObject<TValue>(element, options);
                        }
                        else
                        {
                            // Property is declared, set it using the property's set value task
                            ReadAndSetDeclaredMember(ref reader, property, instance, options);
                        }
                        reader.Read();
                    }
                    return;
                }
                default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
            }
        }

        #endregion

        #region Dynamic

        public object? ReadToObjectOrGetDefault(ref Utf8JsonReader reader, ISerializationMetadata metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }
            return ReadToObjectInternal(ref reader, metadata);
        }

        public object ReadToObject(ref Utf8JsonReader reader, ISerializationMetadata metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new NullReferenceException("Expected a non-null value when reading to object");
            }
            return ReadToObjectInternal(ref reader, metadata);
        }

        public void ReadToObject(ref Utf8JsonReader reader, object instance, ISerializationMetadata metadata)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType == JsonTokenType.StartObject && !reader.Read()
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
            }
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                {
                    // No data to read
                    return;
                }
                case JsonTokenType.PropertyName:
                {
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType is not JsonTokenType.PropertyName || reader.GetString() is not string propertyName)
                            throw new JsonException("Invalid JSON token encountered. Expected property name.");

                        if (metadata.GetProperty(propertyName) is not { } property)
                        {
                            // Encountered an unknown property in input JSON. Skipping.
                            reader.Skip();
                        }
                        else
                        {
                            // Property is declared, set it using the property's set value task
                            ReadAndSetDeclaredMember(ref reader, property, instance, options);
                        }
                        reader.Read();
                    }
                    return;
                }
                default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
            }
        }

        private object ReadToObjectInternal(ref Utf8JsonReader reader, ISerializationMetadata metadata)
        {
            if (metadata.UsesParameterizedConstructor)
            {
                return InvokeParameterizedConstructorAndReadToObject(ref reader, metadata);
            }
            var instance = metadata.InvokeParameterlessConstructor();
            ReadToObject(ref reader, instance, metadata);
            return instance;
        }

        private object InvokeParameterizedConstructorAndReadToObject(ref Utf8JsonReader reader, ISerializationMetadata metadata)
        {
            var assigments = new (DeclaredProperty? property, object? value)[metadata.DeclaredPropertyCount];
            var nextNonConstructorParameterIndex = metadata.ParameterizedConstructorParameterCount;

            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                {
                    // No data to read. This could still work if all parameters are optional:
                    return metadata.InvokeParameterizedConstructor(assigments);
                }
                case JsonTokenType.PropertyName:
                {
                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType is not JsonTokenType.PropertyName || reader.GetString() is not string propertyName)
                            throw new JsonException("Invalid JSON token encountered. Expected property name.");

                        if (metadata.GetProperty(propertyName) is not { } property)
                        {
                            // Encountered an unknown property in input JSON. Skipping.
                            reader.Skip();
                        }
                        else
                        {
                            // Property is declared, get its value and add it to the assignments list at
                            // the index where it ocurs in the constructor (if any) or at the counter otherwise.
                            var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
                            if (property.CustomConstructorParameterInfo?.Position is int index)
                                assigments[index] = (property, value);
                            else
                            {
                                assigments[nextNonConstructorParameterIndex] = (property, value);
                                nextNonConstructorParameterIndex += 1;
                            }
                        }
                        reader.Read();
                    }
                    var instance = metadata.InvokeParameterizedConstructor(assigments);
                    for (var i = metadata.ParameterizedConstructorParameterCount; i < metadata.DeclaredPropertyCount; i += 1)
                    {
                        // Set additional properties
                        var (property, value) = assigments[i];
                        property?.SetValueOrBlock(instance!, value);
                    }
                    return instance;
                }
                default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
            }
        }


        public bool TryReadNextProperty(ref Utf8JsonReader reader, Type propertyType, out string? name, out object? value)
        {
            if
            (
                reader.TokenType == JsonTokenType.None && !reader.Read() ||
                reader.TokenType != JsonTokenType.PropertyName && !reader.Read() ||
                reader.TokenType == JsonTokenType.EndObject
            )
            {
                name = null;
                value = default;
                return false;
            }
            if
            (
                reader.TokenType != JsonTokenType.PropertyName ||
                reader.GetString() is not string propertyName
            )
            {
                throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected PropertyName");
            }

            name = propertyName;
            value = JsonSerializer.Deserialize(ref reader, propertyType, options);
            reader.Read();
            return true;
        }

        private static void ReadAndSetDeclaredMember(ref Utf8JsonReader reader, Property property, object instance, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
            property.SetValueOrBlock(instance, value);
        }

        #endregion
    }
}