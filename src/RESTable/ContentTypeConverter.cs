using System;
using System.Linq;
using Newtonsoft.Json;

namespace RESTable
{
    internal class ContentTypeConverter : JsonConverter<ContentType>
    {
        public override void WriteJson(JsonWriter writer, ContentType value, JsonSerializer s) => writer.WriteValue(value.ToString());

        public override ContentType ReadJson(JsonReader reader, Type o, ContentType e, bool h, JsonSerializer s)
        {
            var contentTypeString = reader.Value as string;
            if (string.IsNullOrWhiteSpace(contentTypeString)) return default;
            return ContentType.ParseMany(contentTypeString).FirstOrDefault();
        }
    }
}