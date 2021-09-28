using System;
using System.Globalization;
using System.Text.Json;

namespace RESTable.Json
{
    internal static class ExtensionMethods
    {
        private static JsonDocument JsonDocumentFromObject(object? value, Type type, JsonSerializerOptions options)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, type, options);
            return JsonDocument.Parse(bytes);
        }

        private static JsonElement JsonElementFromObject<TValue>(TValue? value, JsonSerializerOptions options)
        {
            return JsonElementFromObject(value, typeof(TValue), options);
        }

        private static JsonElement JsonElementFromObject(object? value, Type type, JsonSerializerOptions options)
        {
            using var doc = JsonDocumentFromObject(value, type, options);
            return doc.RootElement.Clone();
        }


        internal static T? ToObject<T>(this JsonElement element, JsonSerializerOptions options)
        {
            Exception exception<TValue>(TValue value)
            {
                throw new InvalidCastException($"Cannot convert JsonElement with kind '{element.ValueKind}' and value '{value}' " +
                                               $"to an instance of '{typeof(T).GetRESTableTypeName()}'");
            }

#if NETSTANDARD2_0
            T? reserialize()
            {
                var json = JsonSerializer.SerializeToUtf8Bytes(element, options);
                return JsonSerializer.Deserialize<T>(json, options);
            }
#else
            T? reserialize()
            {
                var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(bufferWriter))
                    element.WriteTo(writer);
                return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
            }
#endif
            var typeCode = Type.GetTypeCode(typeof(T));

            return element.ValueKind switch
            {
                JsonValueKind.Undefined => default,
                JsonValueKind.Null => default,
                JsonValueKind.String when typeof(T).IsEnum => (T) Enum.Parse(typeof(T), element.GetString()!),
                JsonValueKind.String when element.GetString() is T t => t,
                JsonValueKind.Number => typeCode switch
                {
                    // Match types
                    TypeCode.SByte when element.TryGetSByte(out var v) && v is T t => t,
                    TypeCode.Byte when element.TryGetByte(out var v) && v is T t => t,
                    TypeCode.Int16 when element.TryGetInt16(out var v) && v is T t => t,
                    TypeCode.UInt16 when element.TryGetUInt16(out var v) && v is T t => t,
                    TypeCode.Int32 when element.TryGetInt32(out var v) && v is T t => t,
                    TypeCode.UInt32 when element.TryGetUInt32(out var v) && v is T t => t,
                    TypeCode.Int64 when element.TryGetInt64(out var v) && v is T t => t,
                    TypeCode.UInt64 when element.TryGetUInt64(out var v) && v is T t => t,
                    TypeCode.Single when element.TryGetSingle(out var v) && v is T t => t,
                    TypeCode.Double when element.TryGetDouble(out var v) && v is T t => t,
                    TypeCode.Decimal when element.TryGetDecimal(out var v) && v is T t => t,

                    // If object, either get an int, long or a decimal for it (whatever works first)
                    TypeCode.Object when element.TryGetInt32(out var v) && v is T t => t,
                    TypeCode.Object when element.TryGetInt64(out var v) && v is T t => t,
                    TypeCode.Object when element.TryGetDecimal(out var v) && v is T t => t,

                    // Allow getting numbers as strings
                    TypeCode.String when element.TryGetDecimal(out var v) && v.ToString(CultureInfo.InvariantCulture) is T t => t,

                    // Else fail (don't allow to cast to bool, char etc.
                    _ => throw exception(element.GetRawText())
                },
                JsonValueKind.True when true is T tTrue => tTrue,
                JsonValueKind.True => throw exception(true),
                JsonValueKind.False when false is T tFalse => tFalse,
                JsonValueKind.False => throw exception(false),
                _ => reserialize()
            };
        }

        internal static object? ToObject(this JsonElement element, Type targetType, JsonSerializerOptions options)
        {
            Exception exception<TValue>(TValue value)
            {
                throw new InvalidCastException($"Cannot convert JsonElement with kind '{element.ValueKind}' and value '{value}' " +
                                               $"to an instance of '{targetType.GetRESTableTypeName()}'");
            }

#if NETSTANDARD2_0
            object? reserialize()
            {
                var json = JsonSerializer.SerializeToUtf8Bytes(element, options);
                return JsonSerializer.Deserialize(json, targetType, options);
            }
#else
            object? reserialize()
            {
                var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
                using (var writer = new Utf8JsonWriter(bufferWriter))
                    element.WriteTo(writer);
                return JsonSerializer.Deserialize(bufferWriter.WrittenSpan, targetType, options);
            }
#endif

            var typeCode = Type.GetTypeCode(targetType);

            return element.ValueKind switch
            {
                JsonValueKind.Undefined => default,
                JsonValueKind.Null => default,
                JsonValueKind.String when targetType.IsEnum => Enum.Parse(targetType, element.GetString()!),
                JsonValueKind.String when typeCode is TypeCode.String => element.GetString(),
                JsonValueKind.String when targetType == typeof(object) => element.GetString(),
                JsonValueKind.Number => typeCode switch
                {
                    // Match types
                    TypeCode.SByte when element.TryGetSByte(out var v) => v,
                    TypeCode.Byte when element.TryGetByte(out var v) => v,
                    TypeCode.Int16 when element.TryGetInt16(out var v) => v,
                    TypeCode.UInt16 when element.TryGetUInt16(out var v) => v,
                    TypeCode.Int32 when element.TryGetInt32(out var v) => v,
                    TypeCode.UInt32 when element.TryGetUInt32(out var v) => v,
                    TypeCode.Int64 when element.TryGetInt64(out var v) => v,
                    TypeCode.UInt64 when element.TryGetUInt64(out var v) => v,
                    TypeCode.Single when element.TryGetSingle(out var v) => v,
                    TypeCode.Double when element.TryGetDouble(out var v) => v,
                    TypeCode.Decimal when element.TryGetDecimal(out var v) => v,

                    // If object, either get an int, a long or a decimal for it (whatever works first)
                    TypeCode.Object when element.TryGetInt32(out var v) => v,
                    TypeCode.Object when element.TryGetInt64(out var v) => v,
                    TypeCode.Object when element.TryGetDecimal(out var v) => v,

                    // Allow getting numbers as strings
                    TypeCode.String when element.TryGetDecimal(out var v) && v.ToString(CultureInfo.InvariantCulture) is string s => s,

                    // Else fail (don't allow to cast to bool, char etc.
                    _ => throw exception(element.GetRawText())
                },
                JsonValueKind.True when typeCode == TypeCode.Boolean => true,
                JsonValueKind.True => throw exception(true),
                JsonValueKind.False when typeCode == TypeCode.Boolean => false,
                JsonValueKind.False => throw exception(false),
                _ => reserialize()
            };
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        internal static JsonElement ToJsonElement<T>(this T obj, JsonSerializerOptions options)
        {
            return JsonElementFromObject(obj, options);
        }

        /// <summary>
        /// Converts a Dictionary object to a JSON.net JObject
        /// </summary>
        internal static JsonElement ToJsonElement(this object obj, Type targetType, JsonSerializerOptions options)
        {
            return JsonElementFromObject(obj, targetType, options);
        }
    }
}