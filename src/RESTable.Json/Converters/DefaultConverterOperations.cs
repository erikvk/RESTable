using System;
using System.Collections.Generic;
using System.Text.Json;
using RESTable.Meta;

namespace RESTable.Json.Converters
{
    internal static class DefaultConverterOperations
    {
        internal static void WriteDynamicMembers<T, TKey, TValue>(Utf8JsonWriter writer, T instance, JsonSerializerOptions options)
            where T : IEnumerable<KeyValuePair<TKey, TValue?>>
            where TKey : notnull
        {
            foreach (var (dynamicKey, dynamicValue) in instance)
            {
                var propertyName = dynamicKey is Type type ? type.GetRESTableTypeName() : dynamicKey.ToString();
                writer.WritePropertyName(propertyName!);
                if (dynamicValue is null)
                {
                    // The type is unknown, so we can't call the appropriate converter to 
                    // deal with the null value. Instead we just write null.
                    writer.WriteNullValue();
                }
                else JsonSerializer.Serialize(writer, dynamicValue, dynamicValue.GetType(), options);
            }
        }

        internal static void SerializeDeclaredMembers<T>(Utf8JsonWriter writer, ISerializationMetadata<T> metadata, T value, JsonSerializerOptions options)
        {
            for (var i = 0; i < metadata.PropertiesToSerialize.Length; i += 1)
            {
                var property = metadata.PropertiesToSerialize[i];
                var name = property.Name;
                object? propertyValue;
                var propertyValueTask = property.GetValue(value!);
                if (propertyValueTask.IsCompleted)
                    propertyValue = propertyValueTask.GetAwaiter().GetResult();
                else propertyValue = propertyValueTask.AsTask().Result;
                if (property.HiddenIfNull && propertyValue is null)
                    continue;
                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, propertyValue, property.Type, options);
            }
        }

        internal static void SetDynamicMember<T, TValue>(ref Utf8JsonReader reader, string propertyName, T instance, JsonSerializerOptions options)
            where T : IDictionary<string, TValue?>
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            instance[propertyName] = element.ToObject<TValue?>(options);
        }

        internal static void SetDeclaredMember<T>(ref Utf8JsonReader reader, DeclaredProperty property, T instance, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize(ref reader, property.Type, options);
            var setValueTask = property.SetValue(instance!, value);
            if (setValueTask.IsCompleted)
                setValueTask.GetAwaiter().GetResult();
            else setValueTask.AsTask().Wait();
        }
    }
}