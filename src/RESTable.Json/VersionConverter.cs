using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RESTable.Json
{
    public class VersionConverter : JsonConverter<Version>
    {
        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<JsonElement>(ref reader, options) switch
            {
                {ValueKind: JsonValueKind.Null} => null,
                {ValueKind: JsonValueKind.String} stringElement => Version.Parse(stringElement.GetString()!),
                _ => throw new FormatException("Invalid Version syntax")
            };
        }

        public override void Write(Utf8JsonWriter writer, Version? value, JsonSerializerOptions options)
        {
            if (value is null)
                writer.WriteNullValue();
            else writer.WriteStringValue(value.ToString());
        }

        public override bool HandleNull => true;
    }
}