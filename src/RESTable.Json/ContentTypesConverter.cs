using System;
using Newtonsoft.Json;

namespace RESTable.Json
{
    internal class ContentTypesConverter : JsonConverter<ContentTypes>
    {
        public override void WriteJson(JsonWriter writer, ContentTypes? value, JsonSerializer s) => writer.WriteValue(value?.ToString());

        public override ContentTypes? ReadJson(JsonReader reader, Type o, ContentTypes? e, bool h, JsonSerializer s)
        {
            var contentTypeString = reader.Value as string;
            if (string.IsNullOrWhiteSpace(contentTypeString))
                return default;
            return ContentType.ParseMany(contentTypeString!);
        }
    }
}