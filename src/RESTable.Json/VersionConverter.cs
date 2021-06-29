using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RESTable.Json
{
    public class VersionConverter : JsonConverter<Version>
    {
        public override void WriteJson(JsonWriter writer, Version? value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }

        public override Version? ReadJson(JsonReader reader, Type objectType, Version? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var value = JToken.Load(reader);
            if (value.Type == JTokenType.Null)
                return null;
            if (value.Type == JTokenType.String && value.Value<string>() is string stringValue)
                return Version.Parse(stringValue);
            throw new FormatException("Invalid Version syntax");
        }
    }
}