using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using RESTable.Meta;

namespace RESTable.ContentTypeProviders;

public readonly struct JsonReader
{
    private JsonSerializerOptions Options { get; }
    private IJsonProvider JsonProvider { get; }
    private ArrayPool<(bool, object?)> ArrayPool { get; }

    public JsonReader(JsonSerializerOptions jsonSerializerOptions, IJsonProvider jsonProvider)
    {
        Options = jsonSerializerOptions;
        JsonProvider = jsonProvider;
        ArrayPool = ArrayPool<(bool, object?)>.Shared;
    }

    #region Read properties

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
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected PropertyName");

        name = propertyName;
        value = JsonSerializer.Deserialize<T>(ref reader, Options);
        reader.Read();
        return true;
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
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected PropertyName");

        name = propertyName;
        value = JsonSerializer.Deserialize(ref reader, propertyType, Options);
        reader.Read();
        return true;
    }

    #endregion

    #region Populate existing objects

    public void PopulateObject(ref Utf8JsonReader reader, object instance, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
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
                        // Encountered an unknown property in input JSON. Skipping.
                        reader.Skip();
                    else
                        // Property is declared, set it using the property's set value task
                        PopulateProperty(ref reader, property, instance);
                    reader.Read();
                }
                return;
            }
            default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
        }
    }

    public void PopulateObject<T>(ref Utf8JsonReader reader, T instance, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
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
                        // Encountered an unknown property in input JSON. Skipping.
                        reader.Skip();
                    else
                        // Property is declared, set it using the property's set value task
                        PopulateProperty(ref reader, property, instance!);
                    reader.Read();
                }
                return;
            }
            default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
        }
    }

    public void PopulateDictionary(ref Utf8JsonReader reader, IDictionary instance, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
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
                        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, Options);
                        instance[propertyName] = JsonProvider.ToObject(element, metadata.DictionaryValueType!, Options);
                    }
                    else
                    {
                        // Property is declared, set it using the property's set value task
                        PopulateProperty(ref reader, property, instance);
                    }
                    reader.Read();
                }
                return;
            }
            default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
        }
    }

    public void PopulateDictionary<TValue>(ref Utf8JsonReader reader, IDictionary<string, TValue?> instance, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
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
                        var element = JsonSerializer.Deserialize<JsonElement>(ref reader, Options);
                        instance[propertyName] = JsonProvider.ToObject<TValue>(element, Options);
                    }
                    else
                    {
                        // Property is declared, set it using the property's set value task
                        PopulateProperty(ref reader, property, instance);
                    }
                    reader.Read();
                }
                return;
            }
            default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
        }
    }

    private void PopulateProperty(ref Utf8JsonReader reader, Property property, object instance)
    {
        var value = JsonSerializer.Deserialize(ref reader, property.Type, Options);
        property.SetValueOrBlock(instance, value);
    }

    #endregion

    #region Read to object

    public object? ReadToObjectOrGetDefault(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
        if (reader.TokenType == JsonTokenType.Null) return default;
        return ReadToObjectInternal(ref reader, metadata);
    }

    public object ReadToObject(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
        if (reader.TokenType == JsonTokenType.Null) throw new NullReferenceException("Expected a non-null value when reading to object");
        return ReadToObjectInternal(ref reader, metadata);
    }

    public T? ReadToObjectOrGetDefault<T>(ref Utf8JsonReader reader, ISerializationMetadata<T> metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
        if (reader.TokenType == JsonTokenType.Null) return default;
        return (T) ReadToObjectInternal(ref reader, metadata);
    }

    public T ReadToObject<T>(ref Utf8JsonReader reader, ISerializationMetadata<T> metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered. Expected valid JSON");
        if (reader.TokenType == JsonTokenType.Null) throw new NullReferenceException("Expected a non-null value when reading to object");
        return (T) ReadToObjectInternal(ref reader, metadata);
    }

    public IDictionary? ReadToDictionaryOrGetDefault(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered.");
        if (reader.TokenType == JsonTokenType.Null) return default;
        return ReadToDictionaryInternal(ref reader, metadata);
    }

    public IDictionary ReadToDictionary(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered.");
        if (reader.TokenType == JsonTokenType.Null) throw new NullReferenceException("Expected a non-null value when reading to dictionary");
        return ReadToDictionaryInternal(ref reader, metadata);
    }

    public TDictionary? ReadToDictionaryOrGetDefault<TDictionary, TValue>(ref Utf8JsonReader reader, ISerializationMetadata<TDictionary> metadata)
        where TDictionary : IDictionary<string, TValue?>
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered.");
        if (reader.TokenType == JsonTokenType.Null) return default;
        return (TDictionary) ReadToDictionaryInternal(ref reader, metadata);
    }

    public TDictionary ReadToDictionary<TDictionary, TValue>(ref Utf8JsonReader reader, ISerializationMetadata<TDictionary> metadata)
        where TDictionary : IDictionary<string, TValue?>
    {
        if
        (
            reader.TokenType == JsonTokenType.None && !reader.Read() ||
            reader.TokenType == JsonTokenType.StartObject && !reader.Read()
        )
            throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered.");
        if (reader.TokenType == JsonTokenType.Null) throw new NullReferenceException("Expected a non-null value when reading to dictionary");
        return (TDictionary) ReadToDictionaryInternal(ref reader, metadata);
    }

    #endregion

    private object ReadToObjectInternal(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        if (metadata.UsesParameterizedConstructor) return InvokeParameterizedConstructorAndReadToObject(ref reader, metadata);
        var instance = metadata.InvokeParameterlessConstructor();
        PopulateObject(ref reader, instance, metadata);
        return instance;
    }

    private IDictionary ReadToDictionaryInternal(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        if (metadata.UsesParameterizedConstructor) return InvokeParameterizedConstructorAndReadToDictionary(ref reader, metadata);
        var instance = (IDictionary) metadata.InvokeParameterlessConstructor();
        PopulateDictionary(ref reader, instance, metadata);
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
                        var targetType = property.CustomConstructorParameterInfo?.ParameterType ?? property.Type;
                        var value = JsonSerializer.Deserialize(ref reader, targetType, Options);
                        if (property.CustomConstructorParameterInfo?.Position is int index)
                        {
                            assigments[index] = (property, value);
                        }
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
                    property?.SetValueOrBlock(instance, value);
                }
                return instance;
            }
            default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
        }
    }

    private IDictionary InvokeParameterizedConstructorAndReadToDictionary(ref Utf8JsonReader reader, ISerializationMetadata metadata)
    {
        var assigments = new (DeclaredProperty? property, object? value)[metadata.DeclaredPropertyCount];
        var nextNonConstructorParameterIndex = metadata.ParameterizedConstructorParameterCount;
        var dictionaryMembers = new Dictionary<string, object?>();

        switch (reader.TokenType)
        {
            case JsonTokenType.EndObject:
            {
                // No data to read. This could still work if all parameters are optional:
                return (IDictionary) metadata.InvokeParameterizedConstructor(assigments);
            }
            case JsonTokenType.PropertyName:
            {
                while (reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType is not JsonTokenType.PropertyName || reader.GetString() is not string propertyName)
                        throw new JsonException("Invalid JSON token encountered. Expected property name.");

                    if (metadata.GetProperty(propertyName) is not { } property)
                    {
                        // Encountered a non-declared JSON member. This will be populated to the constructed dictionary later.
                        dictionaryMembers[propertyName] = JsonSerializer.Deserialize(ref reader, metadata.DictionaryValueType!, Options);
                    }
                    else
                    {
                        // Property is declared, get its value and add it to the assignments list at
                        // the index where it ocurs in the constructor (if any) or at the counter otherwise.
                        var targetType = property.CustomConstructorParameterInfo?.ParameterType ?? property.Type;
                        var value = JsonSerializer.Deserialize(ref reader, targetType, Options);
                        if (property.CustomConstructorParameterInfo?.Position is int index)
                        {
                            assigments[index] = (property, value);
                        }
                        else
                        {
                            assigments[nextNonConstructorParameterIndex] = (property, value);
                            nextNonConstructorParameterIndex += 1;
                        }
                    }
                    reader.Read();
                }
                var instance = (IDictionary) metadata.InvokeParameterizedConstructor(assigments);
                for (var i = metadata.ParameterizedConstructorParameterCount; i < metadata.DeclaredPropertyCount; i += 1)
                {
                    // Set additional properties
                    var (property, value) = assigments[i];
                    property?.SetValueOrBlock(instance, value);
                }
                foreach (var (key, value) in dictionaryMembers) instance[key] = value;
                return instance;
            }
            default: throw new JsonException($"Invalid JSON token '{reader.TokenType}' encountered");
        }
    }
}
