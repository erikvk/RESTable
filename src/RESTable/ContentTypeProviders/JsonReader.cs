using System;
using System.Collections.Generic;
using System.Text.Json;
using RESTable.Meta;

namespace RESTable.ContentTypeProviders
{
    public readonly struct JsonReader
    {
        private JsonSerializerOptions options { get; }
        private IJsonProvider JsonProvider { get; }

        public JsonReader(JsonSerializerOptions jsonSerializerOptions, IJsonProvider jsonProvider)
        {
            options = jsonSerializerOptions;
            JsonProvider = jsonProvider;
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
                return default;
            var instance = metadata.CreateInstance();
            ReadToObject(ref reader, instance, metadata);
            return instance;
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
            var instance = metadata.CreateInstance();
            ReadToObject(ref reader, instance, metadata);
            return instance;
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
                            SetDeclaredMember(ref reader, property, instance!, options);
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
            var instance = metadata.CreateInstance();
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
            var instance = metadata.CreateInstance();
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
                            SetDeclaredMember(ref reader, property, instance, options);
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
                return default;
            var instance = metadata.CreateInstance();
            ReadToObject(ref reader, instance!, metadata);

            return instance;
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
            var instance = metadata.CreateInstance();
            ReadToObject(ref reader, instance!, metadata);
            return instance!;
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
                            SetDeclaredMember(ref reader, property, instance, options);
                        }
                        reader.Read();
                    }
                    return;
                }
                default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
            }
        }

        private static void SetDeclaredMember(ref Utf8JsonReader reader, Property property, object instance, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
            var setValueTask = property.SetValue(instance, value);
            if (setValueTask.IsCompleted)
                setValueTask.GetAwaiter().GetResult();
            else setValueTask.AsTask().Wait();
        }

        #endregion
    }
}