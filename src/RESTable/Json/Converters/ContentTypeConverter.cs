using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTable.Json.Converters;

[BuiltInConverter]
internal class ContentTypeConverter : JsonConverter<ContentType>
{
    public override ContentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var contentTypeString = reader.GetString();
        if (string.IsNullOrWhiteSpace(contentTypeString))
            return default;
        return ContentType.ParseMany(contentTypeString).FirstOrDefault();
    }

    public override void Write(Utf8JsonWriter writer, ContentType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}