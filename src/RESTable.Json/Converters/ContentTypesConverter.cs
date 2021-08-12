using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTable.Json.Converters
{
    [BuiltInConverter]
    internal class ContentTypesConverter : JsonConverter<ContentTypes>
    {
        public override ContentTypes? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var contentTypeString = reader.GetString();
            if (string.IsNullOrWhiteSpace(contentTypeString))
                return default;
            return ContentType.ParseMany(contentTypeString!);
        }

        public override void Write(Utf8JsonWriter writer, ContentTypes value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}