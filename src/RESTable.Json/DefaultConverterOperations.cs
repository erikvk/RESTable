﻿using System.Collections.Generic;
using System.Text.Json;
using RESTable.Meta;

namespace RESTable.Json
{
    internal static class DefaultConverterOperations
    {
        internal static void WriteDynamicMembers<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options) where T : IDictionary<string, object?>
        {
            foreach (var (dynamicKey, dynamicValue) in value)
            {
                writer.WritePropertyName(dynamicKey);
                if (dynamicValue is null)
                {
                    // The type is unknown, so we can't call the appropriate converter to 
                    // deal with the null value. Instead we just write null.
                    writer.WriteNullValue();
                }
                else JsonSerializer.Serialize(writer, dynamicValue, dynamicValue.GetType(), options);
            }
        }

        internal static void WriteDeclaredMembers<T>(Utf8JsonWriter writer, DeclaredProperty[] declaredPropertiesArray, T value, JsonSerializerOptions options)
        {
            for (var i = 0; i < declaredPropertiesArray.Length; i += 1)
            {
                var property = declaredPropertiesArray[i];
                var name = property.Name;
                object? propertyValue;
                var propertyValueTask = property.GetValue(value!);
                if (propertyValueTask.IsCompleted)
                    propertyValue = propertyValueTask.GetAwaiter().GetResult();
                else propertyValue = propertyValueTask.AsTask().Result;
                if (property.IsWriteOnly || property.HiddenIfNull && propertyValue is null)
                    continue;
                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, propertyValue, property.Type, options);
            }
        }

        internal static void SetDynamicMember<T>(ref Utf8JsonReader reader, string propertyName, T instance, JsonSerializerOptions options) where T : IDictionary<string, object?>
        {
            var element = JsonSerializer.Deserialize<JsonElement>(ref reader, options);
            instance[propertyName] = element.ToObject<object>(options);
        }

        internal static void SetDeclaredMember<T>(ref Utf8JsonReader reader, DeclaredProperty property, T instance, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize(ref reader, property!.Type, options);
            var setValueTask = property.SetValue(instance!, value);
            if (setValueTask.IsCompleted)
                setValueTask.GetAwaiter().GetResult();
            else setValueTask.AsTask().Wait();
        }
    }
}