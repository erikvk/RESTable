using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.ContentTypeProviders;

public readonly struct JsonWriter
{
    private Utf8JsonWriter Utf8JsonWriter { get; }
    private JsonSerializerOptions Options { get; }

    public JsonWriter(Utf8JsonWriter utf8JsonWriter, JsonSerializerOptions options)
    {
        Utf8JsonWriter = utf8JsonWriter;
        Options = options;
    }

    public void WriteObject(object? value, ISerializationMetadata metadata)
    {
        if (value is null)
        {
            Utf8JsonWriter.WriteNullValue();
            return;
        }
        Utf8JsonWriter.WriteStartObject();
        WriteDeclaredMembers(value, metadata);
        Utf8JsonWriter.WriteEndObject();
    }

    public void WriteDictionary<TKey, TValue>(ICollection<KeyValuePair<TKey, TValue?>>? dictionary, ISerializationMetadata metadata) where TKey : notnull
    {
        if (dictionary is null)
        {
            Utf8JsonWriter.WriteNullValue();
            return;
        }
        Utf8JsonWriter.WriteStartObject();
        WriteDynamicMembers(dictionary);
        WriteDeclaredMembers(dictionary, metadata);
        Utf8JsonWriter.WriteEndObject();
    }

    public void WriteDictionary(IDictionary? dictionary, ISerializationMetadata metadata)
    {
        if (dictionary is null)
        {
            Utf8JsonWriter.WriteNullValue();
            return;
        }
        Utf8JsonWriter.WriteStartObject();
        WriteDynamicMembers(dictionary);
        WriteDeclaredMembers(dictionary, metadata);
        Utf8JsonWriter.WriteEndObject();
    }

    public void WriteDynamicMembers<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue?>> instance) where TKey : notnull
    {
        foreach (var (key, value) in instance) WriteDynamicMember(key, value);
    }

    public void WriteDynamicMembers(IDictionary instance)
    {
        foreach (DictionaryEntry entry in instance) WriteDynamicMember(entry.Key, entry.Value);
    }

    private void WriteDynamicMember(object dynamicKey, object? dynamicValue)
    {
        var propertyName = dynamicKey is Type type ? type.GetRESTableTypeName() : dynamicKey.ToString()!;
        Utf8JsonWriter.WritePropertyName(propertyName);
        if (dynamicValue is null)
            // The type is unknown, so we can't call the appropriate converter to 
            // deal with the null value. Instead we just write null.
            Utf8JsonWriter.WriteNullValue();
        else JsonSerializer.Serialize(Utf8JsonWriter, dynamicValue, dynamicValue.GetType(), Options);
    }

    public void WriteDeclaredMembers(object value, ISerializationMetadata metadata)
    {
        for (var i = 0; i < metadata.PropertiesToSerialize.Length; i += 1)
        {
            var property = metadata.PropertiesToSerialize[i];
            var name = property.Name;
            object? propertyValue;
            var propertyValueTask = property.GetValue(value);
            if (propertyValueTask.IsCompleted)
                propertyValue = propertyValueTask.GetAwaiter().GetResult();
            else propertyValue = propertyValueTask.AsTask().Result;
            if (property.HiddenIfNull && propertyValue is null)
                continue;
            Utf8JsonWriter.WritePropertyName(name);
            JsonSerializer.Serialize(Utf8JsonWriter, propertyValue, property.Type, Options);
        }
    }

    #region Delegating members

    public void Flush()
    {
        Utf8JsonWriter.Flush();
    }

    public Task FlushAsync(CancellationToken cancellationToken = new())
    {
        return Utf8JsonWriter.FlushAsync(cancellationToken);
    }

    public void Reset()
    {
        Utf8JsonWriter.Reset();
    }

    public void Reset(IBufferWriter<byte> bufferWriter)
    {
        Utf8JsonWriter.Reset(bufferWriter);
    }

    public void Reset(Stream utf8Json)
    {
        Utf8JsonWriter.Reset(utf8Json);
    }

    public void WriteBase64String(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> bytes)
    {
        Utf8JsonWriter.WriteBase64String(utf8PropertyName, bytes);
    }

    public void WriteBase64String(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> bytes)
    {
        Utf8JsonWriter.WriteBase64String(propertyName, bytes);
    }

    public void WriteBase64String(string propertyName, ReadOnlySpan<byte> bytes)
    {
        Utf8JsonWriter.WriteBase64String(propertyName, bytes);
    }

    public void WriteBase64String(JsonEncodedText propertyName, ReadOnlySpan<byte> bytes)
    {
        Utf8JsonWriter.WriteBase64String(propertyName, bytes);
    }

    public void WriteBase64StringValue(ReadOnlySpan<byte> bytes)
    {
        Utf8JsonWriter.WriteBase64StringValue(bytes);
    }

    public void WriteBoolean(ReadOnlySpan<byte> utf8PropertyName, bool value)
    {
        Utf8JsonWriter.WriteBoolean(utf8PropertyName, value);
    }

    public void WriteBoolean(ReadOnlySpan<char> propertyName, bool value)
    {
        Utf8JsonWriter.WriteBoolean(propertyName, value);
    }

    public void WriteBoolean(string propertyName, bool value)
    {
        Utf8JsonWriter.WriteBoolean(propertyName, value);
    }

    public void WriteBoolean(JsonEncodedText propertyName, bool value)
    {
        Utf8JsonWriter.WriteBoolean(propertyName, value);
    }

    public void WriteBooleanValue(bool value)
    {
        Utf8JsonWriter.WriteBooleanValue(value);
    }

    public void WriteCommentValue(ReadOnlySpan<byte> utf8Value)
    {
        Utf8JsonWriter.WriteCommentValue(utf8Value);
    }

    public void WriteCommentValue(ReadOnlySpan<char> value)
    {
        Utf8JsonWriter.WriteCommentValue(value);
    }

    public void WriteCommentValue(string value)
    {
        Utf8JsonWriter.WriteCommentValue(value);
    }

    public void WriteEndArray()
    {
        Utf8JsonWriter.WriteEndArray();
    }

    public void WriteEndObject()
    {
        Utf8JsonWriter.WriteEndObject();
    }

    public void WriteNull(ReadOnlySpan<byte> utf8PropertyName)
    {
        Utf8JsonWriter.WriteNull(utf8PropertyName);
    }

    public void WriteNull(ReadOnlySpan<char> propertyName)
    {
        Utf8JsonWriter.WriteNull(propertyName);
    }

    public void WriteNull(string propertyName)
    {
        Utf8JsonWriter.WriteNull(propertyName);
    }

    public void WriteNull(JsonEncodedText propertyName)
    {
        Utf8JsonWriter.WriteNull(propertyName);
    }

    public void WriteNullValue()
    {
        Utf8JsonWriter.WriteNullValue();
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, decimal value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, double value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, int value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, long value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, float value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, uint value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<byte> utf8PropertyName, ulong value)
    {
        Utf8JsonWriter.WriteNumber(utf8PropertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, decimal value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, double value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, int value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, long value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, float value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, uint value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(ReadOnlySpan<char> propertyName, ulong value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, decimal value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, double value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, int value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, long value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, float value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, uint value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(string propertyName, ulong value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, decimal value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, double value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, int value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, long value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, float value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, uint value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumber(JsonEncodedText propertyName, ulong value)
    {
        Utf8JsonWriter.WriteNumber(propertyName, value);
    }

    public void WriteNumberValue(decimal value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(double value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(int value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(long value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(float value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(uint value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WriteNumberValue(ulong value)
    {
        Utf8JsonWriter.WriteNumberValue(value);
    }

    public void WritePropertyName(ReadOnlySpan<byte> utf8PropertyName)
    {
        Utf8JsonWriter.WritePropertyName(utf8PropertyName);
    }

    public void WritePropertyName(ReadOnlySpan<char> propertyName)
    {
        Utf8JsonWriter.WritePropertyName(propertyName);
    }

    public void WritePropertyName(string propertyName)
    {
        Utf8JsonWriter.WritePropertyName(propertyName);
    }

    public void WritePropertyName(JsonEncodedText propertyName)
    {
        Utf8JsonWriter.WritePropertyName(propertyName);
    }
#if NET6_0_OR_GREATER
    public void WriteRawValue(string json, bool skipInputValidation = false)
    {
        Utf8JsonWriter.WriteRawValue(json, skipInputValidation);
    }

    public void WriteRawValue(ReadOnlySpan<byte> utf8Json, bool skipInputValidation = false)
    {
        Utf8JsonWriter.WriteRawValue(utf8Json, skipInputValidation);
    }

    public void WriteRawValue(ReadOnlySpan<char> json, bool skipInputValidation = false)
    {
        Utf8JsonWriter.WriteRawValue(json, skipInputValidation);
    }
#endif
    public void WriteStartArray()
    {
        Utf8JsonWriter.WriteStartArray();
    }

    public void WriteStartArray(ReadOnlySpan<byte> utf8PropertyName)
    {
        Utf8JsonWriter.WriteStartArray(utf8PropertyName);
    }

    public void WriteStartArray(ReadOnlySpan<char> propertyName)
    {
        Utf8JsonWriter.WriteStartArray(propertyName);
    }

    public void WriteStartArray(string propertyName)
    {
        Utf8JsonWriter.WriteStartArray(propertyName);
    }

    public void WriteStartArray(JsonEncodedText propertyName)
    {
        Utf8JsonWriter.WriteStartArray(propertyName);
    }

    public void WriteStartObject()
    {
        Utf8JsonWriter.WriteStartObject();
    }

    public void WriteStartObject(ReadOnlySpan<byte> utf8PropertyName)
    {
        Utf8JsonWriter.WriteStartObject(utf8PropertyName);
    }

    public void WriteStartObject(ReadOnlySpan<char> propertyName)
    {
        Utf8JsonWriter.WriteStartObject(propertyName);
    }

    public void WriteStartObject(string propertyName)
    {
        Utf8JsonWriter.WriteStartObject(propertyName);
    }

    public void WriteStartObject(JsonEncodedText propertyName)
    {
        Utf8JsonWriter.WriteStartObject(propertyName);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, DateTime value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, value);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, DateTimeOffset value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, value);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, Guid value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, value);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<byte> utf8Value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, utf8Value);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, ReadOnlySpan<char> value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, value);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, string? value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, value);
    }

    public void WriteString(ReadOnlySpan<byte> utf8PropertyName, JsonEncodedText value)
    {
        Utf8JsonWriter.WriteString(utf8PropertyName, value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, DateTime value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, DateTimeOffset value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, Guid value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> utf8Value)
    {
        Utf8JsonWriter.WriteString(propertyName, utf8Value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, string? value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(ReadOnlySpan<char> propertyName, JsonEncodedText value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(string propertyName, DateTime value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(string propertyName, DateTimeOffset value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(string propertyName, Guid value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(string propertyName, ReadOnlySpan<byte> utf8Value)
    {
        Utf8JsonWriter.WriteString(propertyName, utf8Value);
    }

    public void WriteString(string propertyName, ReadOnlySpan<char> value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(string propertyName, string? value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(string propertyName, JsonEncodedText value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(JsonEncodedText propertyName, DateTime value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(JsonEncodedText propertyName, DateTimeOffset value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(JsonEncodedText propertyName, Guid value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<byte> utf8Value)
    {
        Utf8JsonWriter.WriteString(propertyName, utf8Value);
    }

    public void WriteString(JsonEncodedText propertyName, ReadOnlySpan<char> value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(JsonEncodedText propertyName, string? value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteString(JsonEncodedText propertyName, JsonEncodedText value)
    {
        Utf8JsonWriter.WriteString(propertyName, value);
    }

    public void WriteStringValue(DateTime value)
    {
        Utf8JsonWriter.WriteStringValue(value);
    }

    public void WriteStringValue(DateTimeOffset value)
    {
        Utf8JsonWriter.WriteStringValue(value);
    }

    public void WriteStringValue(Guid value)
    {
        Utf8JsonWriter.WriteStringValue(value);
    }

    public void WriteStringValue(ReadOnlySpan<byte> utf8Value)
    {
        Utf8JsonWriter.WriteStringValue(utf8Value);
    }

    public void WriteStringValue(ReadOnlySpan<char> value)
    {
        Utf8JsonWriter.WriteStringValue(value);
    }

    public void WriteStringValue(string? value)
    {
        Utf8JsonWriter.WriteStringValue(value);
    }

    public void WriteStringValue(JsonEncodedText value)
    {
        Utf8JsonWriter.WriteStringValue(value);
    }

    #endregion
}