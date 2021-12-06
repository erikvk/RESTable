using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTable.Json.Converters;

/// <inheritdoc />
/// <summary>
///     Converts an object to its string value, by using ToString(), during serialization
/// </summary>
[BuiltInConverter]
public class ToStringConverter : JsonConverter<object>
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}